# Neshan Geocoding API

A .NET Core Web API application that reads addresses from Excel files, uses the Neshan Geocoding API to get locations, and stores the results in a SQL Server database.

## Features

- Upload Excel files (.xlsx, .xls) containing addresses
- Automatic geocoding using Neshan Maps Platform API
- Store results in SQL Server database
- RESTful API endpoints for managing addresses
- Support for Persian/Farsi addresses
- Error handling and logging
- Pagination for address listings
- Statistics and reporting

## Prerequisites

- .NET 7.0 or later
- SQL Server (LocalDB, Express, or Full)
- Neshan Maps Platform API Key

## Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd NeshanGeocodingApi
   ```

2. **Configure the database connection**
   - Update the connection string in `appsettings.json`
   - Default uses LocalDB: `Server=(localdb)\mssqllocaldb;Database=NeshanGeocodingDb;Trusted_Connection=true;MultipleActiveResultSets=true`

3. **Configure Neshan API Key**
   - Get your API key from [Neshan Maps Platform](https://platform.neshan.org/)
   - Update the `ApiKey` in `appsettings.json`

4. **Install dependencies**
   ```bash
   dotnet restore
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

The API will be available at `https://localhost:7001` (or the configured port).

## API Endpoints

### Upload Excel File
```
POST /api/geocoding/upload-excel
Content-Type: multipart/form-data

file: [Excel file]
```

### Geocode Pending Addresses
```
POST /api/geocoding/geocode-pending
```

### Get Addresses
```
GET /api/geocoding/addresses?status=Success&page=1&pageSize=50
```

### Get Single Address
```
GET /api/geocoding/addresses/{id}
```

### Delete Address
```
DELETE /api/geocoding/addresses/{id}
```

### Get Statistics
```
GET /api/geocoding/statistics
```

## Excel File Format

The application supports Excel files with the following column headers:

### English Headers
- `address` or `fulladdress` - Full address (required)
- `province` - Province/State
- `city` - City
- `district` - District/Area
- `street` - Street name
- `alley` - Alley/Street number
- `building` - Building name/number

### Persian Headers
- `آدرس` or `ادرس` - Full address (required)
- `استان` - Province/State
- `شهر` - City
- `منطقه` - District/Area
- `خیابان` - Street name
- `کوچه` - Alley/Street number
- `ساختمان` - Building name/number

## Usage Example

1. **Upload Excel file**
   ```bash
   curl -X POST "https://localhost:7001/api/geocoding/upload-excel" \
        -F "file=@addresses.xlsx"
   ```

2. **Geocode addresses**
   ```bash
   curl -X POST "https://localhost:7001/api/geocoding/geocode-pending"
   ```

3. **Get results**
   ```bash
   curl "https://localhost:7001/api/geocoding/addresses?status=Success"
   ```

## Database Schema

The application creates the following table:

### Addresses Table
- `Id` (int, PK) - Primary key
- `FullAddress` (nvarchar(500)) - Original address
- `Province` (nvarchar(100)) - Province/State
- `City` (nvarchar(100)) - City
- `District` (nvarchar(100)) - District/Area
- `Street` (nvarchar(200)) - Street name
- `Alley` (nvarchar(200)) - Alley/Street number
- `Building` (nvarchar(100)) - Building name/number
- `Latitude` (float) - Geocoded latitude
- `Longitude` (float) - Geocoded longitude
- `GeocodedAddress` (nvarchar(500)) - Formatted address from API
- `Status` (nvarchar(50)) - Success, Failed, or Pending
- `ErrorMessage` (nvarchar(500)) - Error message if failed
- `CreatedAt` (datetime2) - Record creation time
- `UpdatedAt` (datetime2) - Last update time

## Error Handling

The application handles various error scenarios:

- **Excel file format errors** - Invalid file format or structure
- **API rate limiting** - Automatic delays between requests
- **Database errors** - Connection and constraint violations
- **Network errors** - HTTP request failures

## Logging

The application uses structured logging with the following levels:
- **Information** - Successful operations
- **Warning** - Non-critical issues
- **Error** - Failed operations and exceptions

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your-connection-string"
  },
  "Neshan": {
    "ApiKey": "your-api-key"
  }
}
```

## Development

### Adding New Features
1. Create models in the `Models` folder
2. Add services in the `Services` folder
3. Create controllers in the `Controllers` folder
4. Update `Program.cs` for dependency injection

### Database Migrations
```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

## Troubleshooting

### Common Issues

1. **API Key Error**
   - Ensure your Neshan API key is valid
   - Check API key permissions for geocoding service

2. **Database Connection Error**
   - Verify SQL Server is running
   - Check connection string format
   - Ensure database permissions

3. **Excel File Error**
   - Ensure file is valid Excel format (.xlsx or .xls)
   - Check column headers match expected format
   - Verify file is not corrupted

4. **Rate Limiting**
   - The application includes delays between API calls
   - Monitor API usage limits in your Neshan account

## License

This project is licensed under the MIT License.

## Support

For issues related to:
- **Neshan Maps Platform API** - Contact [platform@neshan.org](mailto:platform@neshan.org)
- **Application Issues** - Create an issue in the repository 
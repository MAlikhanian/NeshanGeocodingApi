# Setup Guide - Neshan Geocoding API

## Quick Start

### 1. Prerequisites
- .NET 9.0 SDK or later
- SQL Server (LocalDB, Express, or Full)
- Neshan Maps Platform API Key

### 2. Configuration

#### Database Connection
Update the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=NeshanGeocodingDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

#### Neshan API Key
1. Get your API key from [Neshan Maps Platform](https://platform.neshan.org/)
2. Update the API key in `appsettings.json`:
```json
{
  "Neshan": {
    "ApiKey": "YOUR_ACTUAL_API_KEY_HERE"
  }
}
```

### 3. Run the Application

```bash
# Navigate to the project directory
cd NeshanGeocodingApi

# Restore packages
dotnet restore

# Build the application
dotnet build

# Run the application
dotnet run
```

The API will be available at:
- **API**: https://localhost:7001/api/geocoding
- **Test Page**: https://localhost:7001/index.html

### 4. Test the Application

1. **Using the Web Interface**
   - Open https://localhost:7001/index.html
   - Upload an Excel file with addresses
   - Click "Start Geocoding" to process addresses
   - View results and statistics

2. **Using API Endpoints**
   ```bash
   # Upload Excel file
   curl -X POST "https://localhost:7001/api/geocoding/upload-excel" \
        -F "file=@your-addresses.xlsx"

   # Geocode pending addresses
   curl -X POST "https://localhost:7001/api/geocoding/geocode-pending"

   # Get addresses
   curl "https://localhost:7001/api/geocoding/addresses?status=Success"

   # Get statistics
   curl "https://localhost:7001/api/geocoding/statistics"
   ```

## Excel File Format

Create an Excel file with the following columns:

| Column Header | Description | Required |
|---------------|-------------|----------|
| address | Full address | Yes |
| province | Province/State | No |
| city | City | No |
| district | District/Area | No |
| street | Street name | No |
| alley | Alley/Street number | No |
| building | Building name/number | No |

### Persian Headers (Alternative)
- `آدرس` instead of `address`
- `استان` instead of `province`
- `شهر` instead of `city`
- `منطقه` instead of `district`
- `خیابان` instead of `street`
- `کوچه` instead of `alley`
- `ساختمان` instead of `building`

## Sample Data

Use the provided `sample-addresses.csv` file as a reference for creating your Excel file.

## Troubleshooting

### Common Issues

1. **Database Connection Error**
   - Ensure SQL Server is running
   - Check connection string format
   - For LocalDB: `sqllocaldb start`

2. **API Key Error**
   - Verify your Neshan API key is valid
   - Check API key permissions for geocoding service
   - Ensure the key is properly configured in appsettings.json

3. **Excel File Error**
   - Ensure file is .xlsx or .xls format
   - Check column headers match expected format
   - Verify file is not corrupted

4. **Rate Limiting**
   - The application includes 100ms delays between API calls
   - Monitor your Neshan API usage limits

### Logs

Check the console output for detailed error messages and API responses.

## API Endpoints Reference

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/geocoding/upload-excel` | Upload Excel file with addresses |
| POST | `/api/geocoding/geocode-pending` | Process pending addresses |
| GET | `/api/geocoding/addresses` | Get addresses with pagination |
| GET | `/api/geocoding/addresses/{id}` | Get specific address |
| DELETE | `/api/geocoding/addresses/{id}` | Delete address |
| GET | `/api/geocoding/statistics` | Get processing statistics |

## Database Schema

The application automatically creates the database and tables on first run.

### Addresses Table
- `Id` - Primary key
- `FullAddress` - Original address text
- `Province`, `City`, `District`, `Street`, `Alley`, `Building` - Address components
- `Latitude`, `Longitude` - Geocoded coordinates
- `GeocodedAddress` - Formatted address from API
- `Status` - Success, Failed, or Pending
- `ErrorMessage` - Error details if failed
- `CreatedAt`, `UpdatedAt` - Timestamps

## Support

For issues related to:
- **Neshan Maps Platform API**: Contact platform@neshan.org
- **Application Issues**: Check logs and error messages 
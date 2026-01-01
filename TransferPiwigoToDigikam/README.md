# Piwigo to DigiKam Transfer Tool

This application transfers images from a Piwigo website to a DigiKam collection, preserving categories, tags, and metadata.

## Recent Improvements

- **Robust Network Handling**: Added 30-second timeout and automatic retry logic (3 attempts) for network requests
- **Better Error Reporting**: Enhanced error messages with specific HTTP status codes (404, 403, 401)
- **File Name Sanitization**: Automatic handling of invalid characters in file and folder names
- **Duplicate Prevention**: Automatic file name conflict resolution
- **URL Validation**: Validates Piwigo URL format before attempting connection
- **Empty URL Detection**: Skips images with missing download URLs instead of failing
- **Detailed Progress**: Tracks successful, failed, and skipped images separately

## Features

- Connects to Piwigo website via API
- Downloads all images from all categories
- Creates DigiKam-compatible SQLite database
- Preserves category structure as folders
- Imports tags and image metadata
- Displays real-time progress
- Handles network failures gracefully with automatic retry
- Sanitizes file names to prevent errors

## Setup Instructions

### 1. Install Required NuGet Packages

You need to install the System.Data.SQLite package. You can do this in two ways:

#### Option A: Using NuGet Package Manager Console
```
Install-Package System.Data.SQLite.Core -Version 1.0.118.0
```

#### Option B: Using Visual Studio NuGet Manager
1. Right-click on the project in Solution Explorer
2. Select "Manage NuGet Packages"
3. Search for "System.Data.SQLite.Core"
4. Install version 1.0.118.0 or later

### 2. Add Project References

If the project doesn't build, you may need to manually add references:
1. Right-click on "References" in Solution Explorer
2. Click "Add Reference"
3. Add the following references if missing:
   - System.Web.Extensions (for JSON serialization)
   - System.Data.SQLite (from NuGet package)

### 3. Add New Files to Project

If Visual Studio doesn't automatically include the new files, right-click on the project and select "Reload Project", then:
1. Right-click on the project
2. Select "Add" > "Existing Item"
3. Add the following files:
   - Models\PiwigoImage.cs
   - Services\PiwigoClient.cs
   - Services\DigiKamExporter.cs
   - Services\ImageTransferService.cs

## Usage

1. Run the application
2. Enter your Piwigo website URL (e.g., https://your-site.com)
3. Enter your username and password
4. Select an output directory where the DigiKam collection will be created
5. Click "Start Transfer"
6. Wait for the transfer to complete

The application will automatically:
- Retry failed downloads up to 3 times
- Skip images with missing URLs
- Handle file name conflicts
- Sanitize invalid characters in paths and filenames

## Output

The application creates:
- A folder structure matching your Piwigo categories
- Image files in their respective folders
- A `digikam4.db` SQLite database file in the output directory
- All tags and metadata are preserved

## Opening in DigiKam

1. Open DigiKam
2. Go to Settings > Configure DigiKam > Collections
3. Add a new collection pointing to your output directory
4. DigiKam will recognize the database and display your images with all metadata

## Technical Details

### Piwigo API Methods Used
- `pwg.session.login` - Authentication
- `pwg.categories.getList` - Get all categories
- `pwg.categories.getImages` - Get images from each category
- Image download via direct URL

### DigiKam Database Structure
The application creates a DigiKam 4.x compatible database with the following tables:
- Albums - Folder structure
- Images - Image file records
- ImageInformation - Metadata (dimensions, dates)
- Tags - Tag definitions
- ImageTags - Image-to-tag relationships
- ImageComments - Comments and descriptions
- AlbumRoots - Collection root configuration

## Troubleshooting

### Build Errors

If you get compilation errors about missing types:
1. Make sure System.Data.SQLite.Core NuGet package is installed
2. Check that System.Web.Extensions reference is added
3. Clean and rebuild the solution

### Connection Errors

- Verify your Piwigo URL is correct (include https://)
- Check your username and password
- Ensure your Piwigo site allows API access
- Some sites may require additional authentication

### Transfer Failures

The application now handles most common transfer failures automatically:

**Network Timeouts:**
- Automatically retries up to 3 times with exponential backoff
- Increase timeout if needed for slow connections

**404 Not Found:**
- Image file was deleted or moved on the server
- These will be logged and skipped

**403/401 Access Denied:**
- Authentication issue with the image URL
- Check Piwigo permissions settings

**Empty URL:**
- Some images may not have download URLs in the API response
- These are automatically skipped

**Invalid File Names:**
- The application automatically sanitizes file names
- Invalid characters are replaced with underscores

**File Name Conflicts:**
- Duplicate file names get automatic sequential numbering (file_1.jpg, file_2.jpg, etc.)

### Permission Errors

- Ensure you have write permissions to the output directory
- Run Visual Studio as Administrator if needed

### Why Some Transfers Fail

Based on the user's experience with 89 failures out of 9000:
- Images may have missing or invalid URLs in the Piwigo database
- Some images may have been deleted but metadata still exists
- Network issues during transfer (now handled with retry)
- Invalid characters in file names (now automatically sanitized)
- Permission issues on the Piwigo server

The improved error logging will now show specific reasons for each failure in the status window.

## Requirements

- .NET Framework 4.8.1
- Windows Forms
- Internet connection to access Piwigo site

## License

This is a utility application for personal use.

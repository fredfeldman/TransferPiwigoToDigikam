# Piwigo to DigiKam Transfer Tool

This application transfers images from a Piwigo website to a DigiKam collection, preserving categories, tags, and metadata.

## Features

- Connects to Piwigo website via API
- Downloads all images from all categories
- Creates DigiKam-compatible SQLite database
- Preserves category structure as folders
- Imports tags and image metadata
- Displays real-time progress

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

### Permission Errors

- Ensure you have write permissions to the output directory
- Run Visual Studio as Administrator if needed

## Requirements

- .NET Framework 4.8.1
- Windows Forms
- Internet connection to access Piwigo site

## License

This is a utility application for personal use.

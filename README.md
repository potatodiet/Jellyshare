# Jellyshare

A Jellyfin plugin for sharing libraries between servers.

# Limitations

This plugin is in a very early alpha state. Do not expect stability.

- Is only able to handle Movie libraries.
- Is not able to handle URL or server name changes.
- Is not able to handle library name changes.
- Is only confirmed to support my Apple Silicon Mac with Safari.

# Development

## Requirements

- .NET 6+
- Docker

## Preamble

Create three directories at /DevData/Media/{1,2,3}. Place any number of movies
in each directory. The Blender Foundation has produced many freely licensed
movies which may be useful for this purpose.

## Build

    $ ./build

## Use

Three Jellyfin instances will be running at http://localhost:808{1,2,3}. Access
each instance and go through their installers. Create a Movie library in each
instance using the relevant media folders discussed previously in the preamble.

Create a new API key for each instance. Pick any instance, access its Jellyshare
configuration in the Plugins tab, and insert JSON similar to what's shown below.
Replace any values with your own, as needed. You can repeat this process with
more instances if needed.

    [
    {
        "Address": "http://jellyfin_2:8096/",
        "ApiKey": "618bb6933b1540bf8916a4ca30096ccc"
    },
    {
        "Address": "http://jellyfin_3:8096/",
        "ApiKey": "c4b477a7cd544f1693fb459f0cd51c75"
    }
    ]

Start the Jellyshare Sync task within the Schedules Tasks tab. The task should
take around 10-15 seconds.

If an error occurs at any step, check the latest log file. Feel free to create
an issue with all your relevant information, but remember that this software is
freely provided and I provide no guarantee of support.
# Zif.Reader
C# library to read ZIF files and extract images.

ZIF files are single-file zoomable images. They contain discrete levels of zoom magnification, in which each level is made up of a number of smaller JPEG tiles. Here's a [Microsoft article](https://msdn.microsoft.com/en-us/library/cc645050%28VS.95%29.aspx) about this kind of zoomable image. 

The `ZifReader` class will open a ZIF file on disk, or via a `Stream` or `byte[]`. You can then examine the metadata which describes the different tiles and levels, and you can extract individual tiles or completely assembled levels.

Here's a quick demonstration of usage:

```csharp
using (var zif = new ZifReader())
{
    zif.Load("filename.zif");

    int numberOfLevels = zif.ZoomLevels.Count;
    var smallestLevel = zif.ZoomLevels[0];
    var biggestLevel = zif.ZoomLevels.Last();

    int numberOfTiles = biggestLevel.TileCount;
    var upperLeftTile = biggestLevel.GetTileJpeg(0, 0);

    var entireImageForLevel = biggestLevel.GetImage();
    var biggestImage = zif.GetImage(numberOfLevels - 1);   // Same as entireImageForLevel
    biggestImage.Save("new_filename.png", ImageFormat.Png);
}
```

This project is indebted to Ophir LOJKINE for his [Dezoomify](https://github.com/lovasoa/dezoomify) project, and also for the work he did in making a [JavaScript parser for ZIF](https://github.com/lovasoa/ZIF).

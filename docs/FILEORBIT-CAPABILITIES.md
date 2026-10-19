# FileOrbit capability checklist

Legend: **Built in** works in the app bundle; **Optional engine** is local software FileOrbit can download or use from
your machine; **Partial** covers only the listed directions; **Not implemented** is intentionally not presented as
working. All processing is local. There are no uploads, accounts, subscriptions or quotas.

| Family | Inputs | Implemented outputs / tools | Status |
|---|---|---|---|
| Images | JPG, PNG, WEBP, AVIF, TIFF, BMP, GIF, APNG, ICO, ICNS, JXL, JPEG 2000, PSD/PSB, TGA, PPM, HEIC, QOI, DDS, PCX, MNG, CUR, XCF, camera RAW, DICOM, OpenEXR, HDR and more | Image formats listed at left, PDF, traced SVG; resize, grayscale, rotate, flip, background removal presets | Built in for most routes; HEIC/video-backed routes may need FFmpeg |
| Vector | SVG/SVGZ, EMF, WMF, AI, EPS/PS, CDR, VSD, CGM, ODG/FODG | PDF, raster images, SVG/SVGZ, DXF/DWG, EPS/PS, EMF, WMF, ODG | Partial; Ghostscript or LibreOffice for marked legacy routes |
| PDF | PDF | DOCX, HWPX, TXT, PNG, JPG, TIFF, CBZ, PPTX page images; merge, split, compress, page ranges, margin trimming | Built in for core PDF work; optional Ghostscript can compete for the smallest compressed result |
| Documents | DOCX/DOCM/DOTX, EML/MSG, DOC, ODT/FODT/OTT, RTF, WPS/WPD, Pages and more | PDF, DOCX, HWPX, EPUB, TXT, HTML; MSG → EML; ODT/FODT/OTT | Built in for modern core routes; LibreOffice for many legacy formats |
| Spreadsheets | XLSX/XLSM/XLS/XLT, ODS/OTS/FODS, CSV/TSV, Numbers/ET | PDF, XLSX, ODS, CSV, TSV, JSON, HWPX, HTML | Built in for XLSX/CSV/TSV core; LibreOffice for legacy/vendor formats |
| Presentations | PPTX/PPTM/POTX/PPSX, PPT, ODP/OTP/FODP, Keynote | PDF, PNG, JPG, DOCX, HWPX; OTP | Partial; LibreOffice is needed for many legacy/vendor inputs |
| HWP | HWP, HWPX | PDF, PNG, DOCX, HWP ↔ HWPX, TXT, Markdown, HTML | Built in through Filee's HWP engines |
| Text/markup | TXT, Markdown, HTML, reStructuredText, LaTeX, Org, AsciiDoc, Typst, MediaWiki, DocBook, Jupyter, Textile, man, OPML | PDF, DOCX, HWPX, EPUB, mutual markup conversions, PPTX slides | Markdown core built in; broad markup routes need Pandoc |
| E-books | EPUB, KEPUB, MOBI/AZW family, FB2, CBZ/CBR/CB7, HTMLZ/TXTZ, LIT, LRF, CHM, PDB and more | PDF, EPUB, KEPUB, DOCX, TXT, HTML, HWPX, MOBI, AZW3 | Partial; calibre is required for several proprietary formats |
| Video | MP4, MOV, MKV, WEBM, AVI, WMV, FLV, MPEG, TS/M2TS, 3GP, OGV, VOB and more | MP4 (H.264/HEVC/AV1), WEBM, MOV, MKV, AVI, GIF, APNG, MP3, M4A, 720p, subtitle extraction, every-frame PNG/JPG extraction, VideoToolbox hardware encoding | Optional FFmpeg engine |
| Audio | MP3, M4A/AAC, WAV, FLAC, OGG/OPUS, WMA, AIFF, AMR, APE, WavPack, TTA, DSD, DTS, AC3/E-AC3 and more | MP3, M4A (AAC/ALAC), WAV, FLAC, OGG, OPUS, AAC, WavPack, TTA, W64, AC3/E-AC3 | Optional FFmpeg engine |
| Subtitles | SRT, ASS/SSA, WebVTT, SAMI, LRC, TTML, MicroDVD, SCC | Mutual conversion to UTF-8 subtitle formats | Built in |
| Archives | ZIP, 7Z, RAR, TAR and compressed TAR variants, GZ, ISO, CAB, DMG, ALZ, EGG and more | Safe extraction; ZIP, 7Z, TAR, TAR.GZ; combine arbitrary items into one ZIP | Built in for core routes; 7-Zip package for extended formats; traversal protection retained |
| CAD | DWG, DXF | PDF, SVG, PNG, DWG ↔ DXF | Built in/partial depending on content |
| Fonts | TTF, OTF, WOFF, WOFF2, EOT | Mutual conversion | Built in |
| Structured data | JSON/JSONL, YAML, TOML, XML, vCard | Mutual conversion; CSV/XLSX tables; vCard ↔ CSV | Built in |
| 3D models | GLB, glTF, STL, OBJ/MTL, 3MF, PLY | Mutual conversion and PNG preview | Built in |

## FileOrbit wheels

| Wheel | Default modifier while dragging | What it contains |
|---|---:|---|
| Conversions | Shift | The selected file profile's valid output presets |
| Tools | Command | Applicable resize/rotate/flip/grayscale/background, PDF merge/split/compress/page-image, video frame extraction and archive tools |
| Actions | Control | AirDrop, Shelf, Copy, Move, Copy Path and Open in Terminal |

The menu appears immediately on the modifier tap and stays present when the modifier is released; the Finder drag ends
it. Escape cancels. The Shortcuts page lets you change the modifier, mouse button, scope, gesture kind and selected
wheel. On macOS the menu joins every desktop Space, including separate displays, Stage Manager sets and another app's
full-screen Space, and is positioned within the display containing the pointer.

## Explicit gaps

- Interactive free-form image cropping is **not implemented**; format conversion, resizing, rotation, flips and PDF
  margin trimming are implemented.
- Interactive audio/video timeline trimming is **not implemented**; media format/codec/resolution conversion and
  subtitle extraction are implemented through FFmpeg.
- The global Finder gesture needs Accessibility access and depends on macOS input/drag behavior. The hook does not
  obtain file URLs from raw input events: FileOrbit shows the wheel immediately, pre-fills it from Finder's selection,
  and still requires the live native drop to land on a slice. The main-window drop zone and Finder service are fallback
  paths.
- The delivered development build is ad-hoc signed and not notarized.

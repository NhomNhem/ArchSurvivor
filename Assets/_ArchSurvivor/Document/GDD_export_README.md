GDD Export README

This README explains how to export `GDD.md` to HTML and PDF using pandoc on Windows PowerShell. Your workspace location:

I:\unityVers\ArchSurvivor\Assets\_ArchSurvivor\Document\GDD.md

1) Generate HTML (recommended for quick preview)

Open PowerShell and run:

pandoc "I:\unityVers\ArchSurvivor\Assets\_ArchSurvivor\Document\GDD.md" -s -o "I:\unityVers\ArchSurvivor\Assets\_ArchSurvivor\Document\GDD.html" --css "I:\unityVers\ArchSurvivor\Assets\_ArchSurvivor\Document\gdd.css" --metadata title="ArchSurvivor GDD"

2) Generate PDF using wkhtmltopdf (if installed)

First create HTML as above, then:

wkhtmltopdf "I:\unityVers\ArchSurvivor\Assets\_ArchSurvivor\Document\GDD.html" "I:\unityVers\ArchSurvivor\Assets\_ArchSurvivor\Document\GDD.pdf"

3) Generate PDF using pandoc + pdf-engine (recommended engines: wkhtmltopdf, weasyprint, or pdflatex)

- Using pdflatex (requires a TeX distribution like MiKTeX or TeX Live):

pandoc "I:\unityVers\ArchSurvivor\Assets\_ArchSurvivor\Document\GDD.md" -s -o "I:\unityVers\ArchSurvivor\Assets\_ArchSurvivor\Document\GDD.pdf" --pdf-engine=pdflatex --metadata title="ArchSurvivor GDD"

- Using wkhtmltopdf via pandoc (if wkhtmltopdf supports it):

pandoc "I:\unityVers\ArchSurvivor\Assets\_ArchSurvivor\Document\GDD.md" -s -o "I:\unityVers\ArchSurvivor\Assets\_ArchSurvivor\Document\GDD.pdf" --pdf-engine=wkhtmltopdf --css "I:\unityVers\ArchSurvivor\Assets\_ArchSurvivor\Document\gdd.css"

5) Generate DOCX (Microsoft Word)

Open PowerShell and run the following (or use the included script `export_gdd.ps1`):

pandoc "I:\unityVers\ArchSurvivor\Assets\_ArchSurvivor\Document\GDD.md" -s -o "I:\unityVers\ArchSurvivor\Assets\_ArchSurvivor\Document\GDD.docx" --metadata title="ArchSurvivor GDD"

Or run the PowerShell helper script (recommended):

./export_gdd.ps1            # uses default paths
./export_gdd.ps1 -Open      # opens the generated DOCX after export
./export_gdd.ps1 -Input "C:\path\to\GDD.md" -Output "C:\out\GDD.docx"

6) Common tips

- If pandoc is not installed, download from https://pandoc.org/installing.html and add to PATH.
- For best PDF typography, install a TeX distribution (MiKTeX or TeX Live) and use `--pdf-engine=pdflatex` or `xelatex`.
- If your PDF is missing CSS styling, first export to HTML and inspect it in a browser.

If you want, I can:
- Create a PowerShell script to run the preferred export step for you.
- Generate a styled HTML preview and open it (if you have a browser available).
- Detect installed engines and attempt an automatic export (requires tools to be present).

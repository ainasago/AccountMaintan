# Download external CSS and JS libraries to local
Write-Host "Downloading external library files..."

# Create directories
$libDir = "wwwroot/lib"
if (!(Test-Path $libDir)) {
    New-Item -ItemType Directory -Path $libDir
}

# Download AdminLTE CSS
$adminLteDir = "$libDir/admin-lte/css"
if (!(Test-Path $adminLteDir)) {
    New-Item -ItemType Directory -Path $adminLteDir
}
Write-Host "Downloading AdminLTE CSS..."
Invoke-WebRequest -Uri "https://cdn.jsdelivr.net/npm/admin-lte@3.2/dist/css/adminlte.min.css" -OutFile "$adminLteDir/adminlte.min.css"

# Download Font Awesome CSS
$fontAwesomeDir = "$libDir/font-awesome/css"
if (!(Test-Path $fontAwesomeDir)) {
    New-Item -ItemType Directory -Path $fontAwesomeDir
}
Write-Host "Downloading Font Awesome CSS..."
Invoke-WebRequest -Uri "https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" -OutFile "$fontAwesomeDir/all.min.css"

# Download Quill CSS and JS
$quillDir = "$libDir/quill"
if (!(Test-Path $quillDir)) {
    New-Item -ItemType Directory -Path $quillDir
}
$quillCssDir = "$quillDir/css"
$quillJsDir = "$quillDir/js"
if (!(Test-Path $quillCssDir)) {
    New-Item -ItemType Directory -Path $quillCssDir
}
if (!(Test-Path $quillJsDir)) {
    New-Item -ItemType Directory -Path $quillJsDir
}
Write-Host "Downloading Quill CSS..."
Invoke-WebRequest -Uri "https://cdn.jsdelivr.net/npm/quill@1.3.7/dist/quill.snow.css" -OutFile "$quillCssDir/quill.snow.css"
Write-Host "Downloading Quill JS..."
Invoke-WebRequest -Uri "https://cdn.jsdelivr.net/npm/quill@1.3.7/dist/quill.min.js" -OutFile "$quillJsDir/quill.min.js"

Write-Host "Download completed!"


window.methods = {
    DownloadFile: function downloadFile(filename) {
        location.href += 'api/download_file?fileName=' + filename;
}
};
// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function downloadZip() {
    fetch('api/zip')
        .then((response) => {
            if (response.status != 200) {
                let errorMessage = "Error processing the request... (" + response.status + " " + response.statusText + ")";
                throw new Error(errorMessage);
            } else {
                return response.blob();
            }
        })
        .then((blob) => {
            downloadData('geojsons.zip', blob);
        })
        .catch(error => {
            console.error(error);
        });
}

function downloadData(filenameForDownload, data) {
    var textUrl = URL.createObjectURL(data);
    var element = document.createElement('a');
    element.setAttribute('href', textUrl);
    element.setAttribute('download', filenameForDownload);
    element.style.display = 'none';
    document.body.appendChild(element);
    element.click();
    document.body.removeChild(element);
}
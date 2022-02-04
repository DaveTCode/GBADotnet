(function () {
    document.addEventListener("DOMContentLoaded", function (event) {
        var formContainer = document.getElementById("rom-form-container");
        formContainer.addEventListener("submit", loadRomFile);
    });

    function loadRomFile(event) {
        event.preventDefault();
        var formContainer = document.getElementById("rom-form-container");

        var fileInput = document.getElementById("rom-file");
        if (fileInput.files.length === 1) {
            const formData = new FormData();
            formData.append("roms", fileInput.files[0]);

            fetch("api/v1/rom/load", {
                method: 'POST',
                body: formData,
            }).then((response) => {
                console.log(response);
            });

            formContainer.classList += "d-none";
        }
    }
})();
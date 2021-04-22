var docuwareInfos;

//$(document).ready(function () {
//    $('.text-success, .text-danger').each(function () {
//        $(this).hide();
//    })

//    $("#selectExtraction select").focus(function () { //changer événement déclencheur pour verif connexion pour debug la liste
//        $.getJSON('GetUserExtractions',
//            'userLogin='+ $('#userLogin input').val(),
//            function (data) {
//                $.each($(`#selectExtraction option`), function () {
//                    if (this.value != "newExtraction") {this.remove()}
//                })
//                $.each(data, function (key,value) {
//                    $("#selectExtraction select").append(`<option value="${value}">${value}</option>`);
//                });
//            }
//        )
//    });
    
//    $("#nameExtraction input").blur(function () {
//        pattern = /([a-zA-Z0-9\s_\\.\-\(\):])+/i;
//        if (pattern.test($('#nameExtraction input').val())) {
//            $('#nameExtraction .invalid-feedback').hide();
//        }
//        else {
//            $('#nameExtraction .invalid-feedback').show();
//        }
//    });
    
//});

//function onChangeExtraction() { // ajouter déclanchement si identifiants corrects
//    if ($("#selectExtraction select").val() == 'newExtraction') {
//        $("#nameExtraction input").prop("disabled", false);
//    }
//    else {
//        $("#nameExtraction input").prop("disabled", true);
//        $.getJSON('GetExtraction',
//            'userLogin=' + $('#userLogin input').val() +
//            '&name=' + $('#selectExtraction select').val(),
//            function (data) {
//                console.log(data["name"]);
//                console.log(data["orgnization"]);
//                $(`#nameExtraction input`).val(data["name"]);   //non fonctionnel...
//                updateOrganisations();
//                $(`#selectOrganization option[value="${data["orgnization"]}"]`).prop('selected', true);
//                updateFileCabinets();
//                $(`#selectFleCabinet option[value="${data["fileCabinet"]}"]`).prop('selected', true);
//                updateDialogs();
//                $(`#selectDialog option[value="${data["dialog"]}"]`).prop('selected', true);
//                //remplir tableau avec hierarchie pré-cochée (à voir si on affiche tout les champs)
//            });
//    }
//}

//function updateOrganisations() {
//    $.getJSON('Organizations',
//        'docuwareURI=' + $('#docuwareURI input').val() +
//        '&userLogin=' + $('#userLogin input').val() +
//        '&userPassword=' + $('#userPassword input').val(),
//        function (data) {
//            $("#selectOrganization select").empty();
//            $.each(data, function (key, value) {
//                $("#selectOrganization select").append(`<option value="${value}">${value}</option>`);
//            });
//        });
//}

//function updateFileCabinets() {
//    $.getJSON('FileCabinets',
//        'docuwareURI=' + $('#docuwareURI input').val() +
//        '&userLogin=' + $('#userLogin input').val() +
//        '&userPassword=' + $('#userPassword input').val() +
//        '&organization=' + $('#selectOrganization select').val(),
//        function (data) {
//            $("#selectFleCabinet select").empty();
//            $.each(data, function (key, value) {
//                $("#selectFleCabinet select").append(`<option value="${value}">${value}</option>`);
//            });
//        });
//}

//function updateDialogs() {
//    $.getJSON('Dialogs',
//        'docuwareURI=' + $('#docuwareURI input').val() +
//        '&userLogin=' + $('#userLogin input').val() +
//        '&userPassword=' + $('#userPassword input').val() +
//        '&organization=' + $('#selectOrganization select').val() +
//        '&fileCabinet=' + $('#selectFleCabinet select').val(),
//        function (data) {
//            $("#selectDialog select").empty();
//            $.each(data, function (key, value) {
//                $("#selectDialog select").append(`<option value="${value}">${value}</option>`);
//            });
//        });
//}

//function updateFields() {
//    $.getJSON('Fields',
//        'docuwareURI=' + $('#docuwareURI input').val() +
//        '&userLogin=' + $('#userLogin input').val() +
//        '&userPassword=' + $('#userPassword input').val() +
//        '&organization=' + $('#selectOrganization select').val() +
//        '&fileCabinet=' + $('#selectFleCabinet select').val() +
//        '&dialog=' + $('#selectDialog select').val(),
//        function (data) {
//            //$("").empty();
//            //$.each(data, function (key, value) {
//            //    $("").append(`<option value="${value}">${value}</option>`);     //table row
//            //});
//        });
}
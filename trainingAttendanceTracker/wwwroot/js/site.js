// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(function () {
    $('[data-toggle="tooltip"]').tooltip()
})

$(document).on('click', '.delete-btn', function (e) {
    if (!confirm('Jeste li sigurni da želite obrisati ovaj zapis?')) {
        e.preventDefault();
    }
});

setTimeout(function () {
    $('.alert').fadeOut('slow');
}, 5000);
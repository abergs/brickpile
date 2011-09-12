﻿$(document).ready(function () {
    // Hide error labels when clicked
    $('.field-validation-error').live('click', function () {
        $(this).fadeOut('normal', function() {
            $(this).remove();
        });
    });

    // Disable the submit button until all fields is filled
    var $input = $('form fieldset input'), $register = $('input:submit');
    $register.attr('disabled', true);
    $input.keyup(function () {
        var trigger = false;
        $input.each(function () {
            if (!$(this).val()) {
                trigger = true;
            }
        });
        trigger ? $register.attr('disabled', true) : $register.removeAttr('disabled');
    });
});
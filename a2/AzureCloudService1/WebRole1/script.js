//AJAX call to searchForPrefix in the ASMX file
function sendReq(prf) {
    $.ajax({
        type: "POST",
        url: "getQuerySuggestions.asmx/searchForPrefix",
        data: JSON.stringify({ prefix: prf }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (result) {
            console.log('success');
            $('#res').html(""); //clears the div
            var obj = JSON.parse(result.d);
            var block = $("<div>").addClass("sblock");
            // iterate over the array and build the list
            for (var i = 0; i < obj.length; i++) {
                block.append("<div>" + prf + "<b>" + obj[i] + "</b>" + "</div>");
            }
            $("#res").append(block);
        },
        error: function (msg) {
            console.log('error');
        }
    });
}

//call sendReq() every time the input changes 
$(document).ready(function () {
    $('#searchbar').on('input', function () {
        sendReq(this.value);
    });
});


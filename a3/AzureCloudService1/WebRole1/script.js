//AJAX call to getStates() in the ASMX file
function callStats() {
    $.ajax({
        type: "POST",
        url: "admin.asmx/getStats",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (result) {
            console.log('success');
            //$('#res').html(""); //clears the div
            //var obj = JSON.parse(result.d);
            //var cleanInput = prf.trim().toLowerCase();

            //var block = $("<div>").addClass("sblock");
            // iterate over the array and build the list
            //for (var i = 0; i < obj.length; i++) {
            //block.append("<div>" + cleanInput + "<b>" + obj[i] + "</b>" + "</div>");
            //}
            //if (cleanInput != "" && obj.length == 0) {
            //block.append("<div class='msg'><i>No suggestions found</i></div>");
            //}
            //$("#res").append(block);
            //},
            error: function (msg) {
                console.log('error');
            }
        }});
}



//call sendReq() every time the input changes 
$(document).ready(function () {
    while(true) {
        callStats();
    }
});

$(document).ready(function () {
    $('#run-button').click(function() {
        $.ajax({
            type: "POST",
            url: "admin.asmx/startCrawling",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                // Replace the div's content with the page method's return.
                console.log(msg);
            }
        });
    });


    $('#stop-button').click(function() {
    
    
    });
    $('#clear-button').click(function() {
    
    
    });
});
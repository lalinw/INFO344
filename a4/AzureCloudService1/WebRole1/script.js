//AJAX call to searchForPrefix in the ASMX file
function sendReq(prf) {
    $.ajax({
        type: "POST",
        url: "getQuerySuggestions.asmx/searchForPrefix",
        data: JSON.stringify({ input: prf }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (result) {
            console.log('success');
            $('#res').html(""); //clears the div
            var obj = JSON.parse(result.d);
            var cleanInput = prf.trim().toLowerCase();
            
            var block = $("<div>").addClass("sblock");
            // iterate over the array and build the list
            for (var i = 0; i < obj.length; i++) {
                block.append("<div>" + cleanInput + "<b>" + obj[i] + "</b>" + "</div>");
            }
            if (cleanInput != "" && obj.length == 0) {
                block.append("<div class='msg'><i>No suggestions found</i></div>");
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
        //sendReq(this.value);
    });

    $('#submitbutton').click(function () {
        lookUpPlayer($('#searchbar').val());
    });
});

function lookUpPlayer(player) {
    var name = player.trim().replace(" ", "+");
    console.log(name);
    //turn "jeremy lin" to "jeremy+lin"

    $.ajax({
        crossDomain: true,
        contentType: "application/json; charset=utf-8",
        url: "http://ec2-52-38-84-159.us-west-2.compute.amazonaws.com/nbaplayer_jsonp.php?search=" + name,
        data: {},
        dataType: "jsonp",
        success: function (result) {
            var data = result[0];
            console.log(data);
        }
    });
}
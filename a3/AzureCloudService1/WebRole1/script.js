//AJAX call to getStates() in the ASMX file
function callStats() {
    $.ajax({
        type: "POST",
        url: "admin.asmx/getStats",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (result) {
            console.log('success');
            
            var stats = JSON.parse(result.d);
            console.log(stats);

            $('#state').html(stats[0]);
            $('#cpu').html(stats[1]);
            $('#ram').html(stats[2]);
            $('#linksinqueue').html(stats[3]);
            $('#tablesize').html(stats[4]);
            $('#totalurls').html(stats[5]);
  
            $('#tenlinks').html("");
            var linksStr = stats[6];
            var links = linksStr.split(',');
            var lnk = $("<div>");
            for (var i = 0; i < links.size; i++) {
                lnk.append("<p>" + links[i] + "</p>");
                console.log(links[i]);
            }
            
            $('#tenlinks').append(lnk);


            //$('#errorlinks').html("");
            var err = $("<div>");
            var errorsStr = stats[7];
            var errors = errorsStr.split(',');

            $('#errorlinks');
        },
        error: function (msg) {
            console.log('error');
        }
    });
};
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



$(document).ready(function () {

    $('#run-button').click(function () {
        console.log("clicked run button");
        $.ajax({
            type: "POST",
            url: "admin.asmx/startCrawling",
            success: function (msg) {
                // Replace the div's content with the page method's return.
                console.log(msg);
            }
        });
        alert("Crawler Started");
    });


    $('#stop-button').click(function() {
        console.log("clicked stop button");
        $.ajax({
            type: "POST",
            url: "admin.asmx/stopCrawling",
            success: function (msg) {
                // Replace the div's content with the page method's return.
                console.log(msg);
            }
        });
        alert("Crawler is preparing to stop. Please wait.");
    
    });
    $('#clear-button').click(function() {
        console.log("clicked clear button");
        $.ajax({
            type: "POST",
            url: "admin.asmx/clearIndex",
            success: function (msg) {
                // Replace the div's content with the page method's return.
                console.log(msg);
            }
        });
        alert("Table Cleared, Queue Deleted and Counters reset");
    
    });

    $(function () {
        setTimeout(makeStatsCall, 1000)
    });

    function makeStatsCall() {
        setInterval(callStats, 300);
    }

    $('#find').click(function () {
        console.log('searching for title');
        $.ajax({
            type: "POST",
            url: "admin.asmx/getPagetitle",
            data: JSON.stringify({ url: $('#searchtitle').value }),
            dataType: "json",
            success: function (result) {
                // Replace the div's content with the page method's return.
                $('#results').html(result.d);
            },
            error: function (msg) {
                $('#results').html("No results to display");
            }
        });

    })

});
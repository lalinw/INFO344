//AJAX call to getStates() in the ASMX file
//display stats
function callStats() {
    $.ajax({
        type: "POST",
        url: "admin.asmx/getStats",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (result) {
            console.log('success');

            var stats = JSON.parse(result.d);
            //console.log(stats);

            $('#state').html(stats[0]);
            $('#cpu').html(stats[1]);
            $('#ram').html(stats[2] + " MB");
            $('#linksinqueue').html(stats[3]);
            $('#tablesize').html(stats[4]);
            $('#totalurls').html(stats[5]);
            
            //last 10 links
            $('#tenlinks').html("");
            var linkStr = stats[6];
            var links = linkStr.split(',');
            var lnk = $("<ul>");
            for (var i = 0; i < links.length; i++) {
                lnk.append("<li>" + links[i] + "</li>");
            }
            if (linkStr != "") {
                $('#tenlinks').append(lnk);
            }
            //links for error pages
            $('#errorlinks').html("");
            var errorStr = stats[7];
            var err = $("<ul>");
            var errors = errorStr.split(',');
            for (var i = 0; i < errors.length; i++) {
                err.append("<li>" + errors[i] + "</li>");
            }
            if (errorStr != "") {
                $('#errorlinks').append(err);
            }
        },
        error: function (msg) {
            console.log('error');
        }
    });
};

//controls and calling the stats function 
$(document).ready(function () {
    $('#run-button').click(function () {
        console.log("clicked run button");
        $.ajax({
            type: "POST",
            url: "admin.asmx/startCrawling",
            contentType: "application/json; charset=utf-8",
            success: function (msg) {
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
            contentType: "application/json; charset=utf-8",
            success: function (msg) {
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
            contentType: "application/json; charset=utf-8",
            success: function (msg) {
                console.log(msg);
            }
        });
        alert("Table Cleared, Queue Deleted and Counters reset");
    });

    $(function () {
        setTimeout(makeStatsCall, 2000);
    });

    function makeStatsCall() {
        setInterval(callStats, 500);
    }

    $('#find').click(function () {
        var theurl = $('#searchtitle').val().trim();
        console.log('searching for title');
        $.ajax({
            type: "POST",
            url: "admin.asmx/getPageTitle",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify({ url: theurl }),
            dataType: "json",
            success: function (result) {
                $('#results').html(result.d);
            },
            error: function (msg) {
                $('#results').html("No results to display");
            }
        });
    });
    

});
//AJAX call to getStates() in the ASMX file
//display stats
function callStats() {
    $.ajax({
        type: "POST",
        url: "admin.asmx/getStats",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (result) {
            //console.log('success');

            var stats = JSON.parse(result.d);
            //console.log(stats);
            if (stats[0] == "Loading" || stats[0] == "Crawling") {
                $('#graph').html("");
                $('#graph').append("<img src=\"running.gif\"/>");
            }
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
            $('#titlescount').html(stats[8]);
            $('#lasttitle').html(stats[9]);
        },
        error: function (msg) {
            //console.log('error');
        }
    });
};

//controls and calling the stats function 
$(document).ready(function () {
    $('#run-button').click(function () {
        consoleMsg("Running the crawler...");
        $.ajax({
            type: "POST",
            url: "admin.asmx/startCrawling",
            contentType: "application/json; charset=utf-8",
            success: function (msg) {
               //console.log(msg);
                $('#graph').html("");
                $('#graph').append("<img src=\"running.gif\"/>");
            }
        });
    });


    $('#stop-button').click(function () {
        consoleMsg("Crawler stopping...");
        $.ajax({
            type: "POST",
            url: "admin.asmx/stopCrawling",
            contentType: "application/json; charset=utf-8",
            success: function (msg) {
                //console.log(msg);
                $('#graph').html("");
                $('#graph').append("<img src=\"stopped.gif\"/>");
            }
        });
    });
    $('#clear-button').click(function () {
        consoleMsg("Index Cleared!");
        $.ajax({
            type: "POST",
            url: "admin.asmx/clearIndex",
            contentType: "application/json; charset=utf-8",
            success: function (msg) {
                //console.log(msg);
                $('#graph').html("");
                $('#graph').append("<img src=\"stopped.gif\"/>");
            }
        });
    });

    $(function () {
        setTimeout(makeStatsCall, 1000);
    });

    function makeStatsCall() {
        setInterval(callStats, 1000);
    }

    $('#find').click(function () {
        var theurl = $('#searchtitle').val().trim();
        //console.log('searching for title');
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

    $('#dl-button').click(function () {
        $('#dl-button').addClass("disabled");
        consoleMsg("Downloading file...");
        //console.log("click dl");
        $.ajax({
            type: "POST",
            url: "getQuerySuggestions.asmx/downloadTitles",
            contentType: "application/json; charset=utf-8",
            success: function (msg) {
                consoleMsg(msg.d);
            }
       });
    });

    $('#build-button').click(function () {
        consoleMsg("Building Trie...");
        $.ajax({
            type: "POST",
            url: "getQuerySuggestions.asmx/buildTrie",
            contentType: "application/json; charset=utf-8",
            success: function (msg) {
               //console.log("build success");
               consoleMsg("Trie built!");
           }
        });
    });
    

});

function consoleMsg(msg) {
    $('#console-box').html("");
    $('#console-box').append("<div id='console-msg'>" + msg + "</div>");
    $('#console-box').fadeIn(750);
    setTimeout(function () { $('#console-box').fadeOut(750); }, 2500);
}
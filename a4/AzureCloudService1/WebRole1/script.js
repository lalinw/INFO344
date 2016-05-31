﻿//AJAX call to searchForPrefix in the ASMX file
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
    var firstSearch = true;


    $('#searchbar').on('input', function () {
        //sendReq(this.value);
    });

    $('#submitbutton').click(function () {
        if (firstSearch) {
            $("#logoandsearch").animate({ marginTop: "-=30%" }, 750, "swing", function () { });
            firstSearch = false;
        }
        lookUp($('#searchbar').val());
        searchPage($('#searchbar').val());
    });

    
});

function lookUp(userinput) {
    var cleanInput = userinput.trim()
    var name = cleanInput.replace(" ", "+");
    console.log(name);
    
    //turn "jeremy lin" to "jeremy+lin"
    $("#yousearched").html("");
    $("#yousearched").append("<b><i>you searched: </b></i>" + cleanInput);

    $.ajax({
        crossDomain: true,
        contentType: "application/json; charset=utf-8",
        url: "http://ec2-52-38-84-159.us-west-2.compute.amazonaws.com/nbaplayer_jsonp.php?search=" + name,
        data: {},
        dataType: "jsonp",
        success: function (result) {
            $("#namecardresult").html("");
            var data = result[0];
            console.log(data);
            if (data != null) {
                var block = $("<div>").addClass("playerblock");
                $("#namecardresult").append("<img class='profilepic' itemprop='image' src='http://i.cdn.turner.com/nba/nba/.element/img/2.0/sect/statscube/players/large/" + data.FirstName + "_" + data.LastName + ".png' onerror=\"this.onerror=null;" + "this.src='http://i.cdn.turner.com/nba/nba/.element/img/2.0/sect/statscube/players/large/default_nba_headshot_v2.png';\".>");
                block.append("<div id='pname'><h2>" + data.Name +"</h2></div>");
                block.append("<div id='mainstats1'>" + "<span class='var'><h4>Team</h4></span>" + "<span class='data'>" + data.Team + "</span>" + "</div>");
                block.append("<div id='mainstats2'>" + "<span class='var'><h4>GP</h4></span>" + "<span class='data'>" + data.GP + "</span>" + "</div>");
                $("#namecardresult").append(block);
            }
            
        }
    });
}

function searchPage(userinput) {
    $.ajax({
        type: "POST",
        url: "admin.asmx/getSearchResults",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify({ input: userinput }),
        dataType: "json",
        success: function (result) {
            $("#linkresult").html("");
            console.log('success');
            var searchResults = JSON.parse(result.d);
            console.log(searchResults);
            var block = $("<div>");
            for (var i = 0; i < searchResults.length; i++) {
                block.append("<div class='one-link-result'>");
                block.append("<div class='linkresult-title'><a href=" + searchResults[i].Item4 + ">" + searchResults[i].Item3 + "</a></div>");
                block.append("<div class='linkresult-url'>" + searchResults[i].Item4 + "</div>");
                block.append("</div>");
            }
            if (searchResults.length == 0) {
                block.append("<div class='msg'><i>No relevant pages to retrieve</i></div>");
            }
            $("#linkresult").append(block);
        },
        error: function (msg) {
            console.log('error');
        }
    });
};
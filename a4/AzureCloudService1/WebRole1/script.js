var firstSearch = true;

$(document).ready(function () {
    $('#searchbar').on('input', function () {
        sendReq(this.value);
    });

    $('#submitbutton').click(function () {
        $('#suggestions').html("");
        if (firstSearch) {
            $("#logoandsearch").animate({ marginTop: "-=28%" }, 750, "swing", function () { });
            firstSearch = false;
        }
        lookUp($('#searchbar').val());
        searchPage($('#searchbar').val());
    });

});

//AJAX call to searchForPrefix in the ASMX file
function sendReq(prf) {
    $.ajax({
        type: "POST",
        url: "getQuerySuggestions.asmx/searchForPrefix",
        data: JSON.stringify({ input: prf }),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (result) {
            console.log(result.d);
            $('#suggestions').html(""); //clears the div
            var obj = JSON.parse(result.d);
            var cleanInput = prf.trim().toLowerCase();
            
            var block = $("<div>").addClass("suggestionblock");
            // iterate over the array and build the list
            for (var i = 0; i < obj.length; i++) {
                var wholeword = cleanInput + obj[i];
                block.append("<div class='suggested' onclick='searchFromSuggestions(\""+ wholeword +"\")'>" + cleanInput + "<b>" + obj[i] + "</b>" + "</div>");
            }
            if (cleanInput != "" && obj.length == 0) {
                block.append("<div class='msg'><i>No suggestions found</i></div>");
            }
            $("#suggestions").append(block);
            if (cleanInput == "") {
                $('#suggestions').html("");
            }
        },
        error: function (msg) {
            console.log('error');
        }
    });
    setTimeout(function () { $('.suggestionblock').fadeOut(300); console.log("fading?") }, 3000);
    
}

function searchFromSuggestions(input) {
    if (firstSearch) {
        $("#logoandsearch").animate({ marginTop: "-=28%" }, 750, "swing", function () { });
        firstSearch = false;
    }
    $('#searchbar').value = input;
    lookUp(input);
    searchPage(input);

}

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
                block.append("<div id='pname'><h2>" + data.Name + " (" + data.Team +  ")</h2></div>");
                block.append("<div class='mainstats' style='display: inline-block; padding-left: 5%; padding-right: 5%; text-align: center; padding-bottom:5%;'>" + "<span class='var'><h4>PPG</h4></span>" + "<span class='data'>" + data.PPG + "</span>" + "</div>");
                block.append("<div class='mainstats' style='display: inline-block; padding-left: 5%; padding-right: 5%; text-align: center;padding-bottom:5%;'>" + "<span class='var'><h4>GP</h4></span>" + "<span class='data'>" + data.GP + "</span>" + "</div>");
                block.append("<div class='mainstats' style='display: inline-block; padding-left: 5%; padding-right: 5%; text-align: center;padding-bottom:5%;'>" + "<span class='var'><h4>Min</h4></span>" + "<span class='data'>" + data.Min + "</span>" + "</div>");
                block.append("<div><b>Ast: </b>" + data.Ast + "</div>");
                block.append("<div><b>Stl: </b>" + data.Stl + "</div>");
                block.append("<div><b>Blk: </b>" + data.Blk + "</div>");
                block.append("<div><b>TO: </b>" + data.TO + "</div>");
                block.append("<div><b>Reb: </b>" + data.Rebounds_Off + "/" + data.Rebounds_Def + "</div>");
                block.append("<div><b>3pt: </b>" + data.M_3PT + " (" + data.Pct_3PT + "%)</div>");
                block.append("<div><b>FG: </b>" + data.M_FG + " (" + data.Pct_FG + "%)</div>");
                block.append("<div><b>FT: </b>" + data.M_FT + " (" + data.Pct_FT + "%)</div>");
                $("#namecardresult").append(block);
            }
            
        }
    });
}s

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
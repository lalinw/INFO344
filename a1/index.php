<html>
<head>
    <meta charset="UTF-8">
    <title>Document</title>
    
    <link rel="stylesheet" type="text/css" href="main.css">
</head>
<body>
    <div class="search" id="searchBar">
        <img src="NBA_logo.png" alt="NBA" height="150" width="">
        <form>
            <input type="text" name="search">
            <input type="submit" value="Submit">
        </form>
    </div>
    <div class="search" id="searchFeedback">
        <?php
            if(isset($_REQUEST['search']) && $_REQUEST['search'] != "") {
                $playerSearch = $_REQUEST['search'];    //user form input 
                echo 'You searched \'<b>'.$playerSearch.'\'</b>'; //some user feedback
            }
        ?>
    </div>
    <div class="search" id="searchResponse">
    <?php
        require 'classPlayer.php';  //use require instead of include; stops if the class does not exist
        
        //connecting to the DB in RDS
        $conn = new PDO('mysql:host=nba-db.c6uuvnaayrmz.us-west-2.rds.amazonaws.com:3306;dbname=nbadb', 'info344user', '344password'); 
        //check connection
        $conn->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);

        if(isset($_REQUEST['search']) && $_REQUEST['search'] != "") {
            //prepare SQL statement
            $sql = "SELECT * 
                FROM player_stats 
                WHERE Name LIKE '%".$playerSearch."%'";
            $result = $conn->query($sql)->fetchAll(PDO::FETCH_ASSOC);   //query the db

            //create object from query    
            if (count($result) <= 0 ){
                echo "No results to display";
            } else {
                $resultPlayerArray = array();   //array of Player objects
                for($i = 0; $i < count($result); $i++) {
                    //Player($name, $team, $ppg, $m3pt, $reb, $ast, $stl, $blk, $to)
                    //creates new Player object and pushes to array
                    $plyr = new Player(
                        $result[$i]['Name'],
                        $result[$i]['Team'],
                        $result[$i]['PPG'],
                        $result[$i]['M_3PT'], //3pt made
                        $result[$i]['Rebounds_Tot'],
                        $result[$i]['Ast'],
                        $result[$i]['Stl'],
                        $result[$i]['Blk'],
                        $result[$i]['TO']);
                    array_push($resultPlayerArray, $plyr);
                }
                for($i = 0; $i < count($resultPlayerArray); $i++) {       
                    //get player photo + info
                    $nm = $resultPlayerArray[$i]->getName();
                    $nm1 = str_replace(".", "", strtolower(explode(" ", $nm)[0]));
                    $nm2 = strtolower(explode(" ", $nm)[1]);
                    echo "<div class='playerResult'>";
                        echo "<div class='profileImg'>";
                            echo "<img class='profilepic' itemprop='image' 
                                src='http://i.cdn.turner.com/nba/nba/.element/img/2.0/sect/statscube/players/large/".$nm1."_".$nm2.".png' 
                                onerror=\"this.onerror=null;"."this.src='http://i.cdn.turner.com/nba/nba/.element/img/2.0/sect/statscube/players/large/default_nba_headshot_v2.png';\".
                                >";
                        echo "</div>";    
                        echo "<div class='playerName'>";    
                            echo $resultPlayerArray[$i]->getName();
                        echo "</div>";        
                        echo "<div class='playerTeam'>";
                            echo $resultPlayerArray[$i]->getTeam();
                        echo "</div>";
                        echo "<div class='playerInfo'>";
                            // echo "PPG: ".$resultPlayerArray[$i]->getPpg()."/ Assist: ".$resultPlayerArray[$i]->getAst();
                            echo "<div class='statName'>";
                                echo "PPG:<br>Ast:<br>Stl:<br>Blk:<br>TO:";
                            echo "</div>";
                            echo "<div class='stats'>";
                                echo $resultPlayerArray[$i]->getPpg().
                                "<br>".
                                $resultPlayerArray[$i]->getAst().
                                "<br>".
                                $resultPlayerArray[$i]->getStl().
                                "<br>".
                                $resultPlayerArray[$i]->getBlk().
                                "<br>".
                                $resultPlayerArray[$i]->getTurnover();
                            echo "</div>";              
                        echo "</div>";        
                        
                    echo "</div>";
                }
            }
        }
    ?>

</body>
</html>


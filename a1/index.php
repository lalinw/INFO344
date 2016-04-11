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
            <input type="submit" value="search player">
        </form>
    </div>
    <div class="search" id="searchFeedback">
        <?php
            if(isset($_GET['search']) && $_GET['search'] != "") {
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

        if(isset($_GET['search']) && $_GET['search'] != "") {
            //prepare SQL statement 
            // $sql = "SELECT * 
            //     FROM player_stats 
            //     WHERE Name LIKE '%".$playerSearch."%'";
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
                    //Player($name, $team, $ppg, $ast, $stl, $blk, $to)
                    //creates new Player object and pushes to array
                    $plyr = new Player(
                        $result[$i]['Name'],
                        $result[$i]['Team'],
                        $result[$i]['PPG'],
                        $result[$i]['Ast'],
                        $result[$i]['Stl'],
                        $result[$i]['Blk'],
                        $result[$i]['TO'],
                        $result[$i]['GP'],
                        $result[$i]['Min'],
                        array($result[$i]['M_3PT'], $result[$i]['A_3PT'], $result[$i]['Pct_3PT']), //3pt made
                        array($result[$i]['M_FG'], $result[$i]['A_FG'], $result[$i]['Pct_FG']),
                        array($result[$i]['M_FT'], $result[$i]['A_FT'], $result[$i]['Pct_FT']),
                        array($result[$i]['Rebounds_Off'], $result[$i]['Rebounds_Def'], $result[$i]['Rebounds_Tot']));
                    array_push($resultPlayerArray, $plyr);
                    
                }
                for($i = 0; $i < count($resultPlayerArray); $i++) {       
                    //get player photo + info
                    $nm = $resultPlayerArray[$i]->getName();
                    $nm1 = str_replace(".", "", strtolower(explode(" ", $nm)[0]));
                    $nm2 = strtolower(explode(" ", $nm)[1]);
                    //each namecard
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
                            // column 1
                            echo "<div class='col1'>";
                                echo "<div class='statName'>";
                                    echo "Ast:<br>Stl:<br>Blk:<br>TO:";
                                echo "</div>";
                                echo "<div class='stats'>";
                                    echo $resultPlayerArray[$i]->getAst().
                                    "<br>".
                                    $resultPlayerArray[$i]->getStl().
                                    "<br>".
                                    $resultPlayerArray[$i]->getBlk().
                                    "<br>".
                                    $resultPlayerArray[$i]->getTurnover();
                                echo "</div>";
                            echo "</div>";
                            //column 2
                            echo "<div class='col2'>";
                                echo "<div class='statName'>";
                                    echo "Rebounds:<br><br>3pt:<br>FG:<br>FT:";
                                echo "</div>";
                                echo "<div class='stats'>";
                                    echo "Off ".$resultPlayerArray[$i]->getReb()[0]."/Def ".$resultPlayerArray[$i]->getReb()[1].
                                        "<br>".
                                        "Total ".$resultPlayerArray[$i]->getReb()[2]."<br>";
                                    echo $resultPlayerArray[$i]->getPt3()[0]." (".$resultPlayerArray[$i]->getPt3()[2]."%)"."<br>";
                                    echo $resultPlayerArray[$i]->getFg()[0]." (".$resultPlayerArray[$i]->getFg()[2]."%)"."<br>";
                                    echo $resultPlayerArray[$i]->getFt()[0]." (".$resultPlayerArray[$i]->getFt()[2]."%)";
                                echo "</div>";
                                
                            echo "</div>";  
                            
                            //column 3
                            echo "<div class='col3'>";
                                echo "PPG";
                                echo "<div class='box ppg'>".$resultPlayerArray[$i]->getPpg()."</div>";
                                echo "GP";
                                echo "<div class='box gp'>".$resultPlayerArray[$i]->getGp()."</div>";
                                echo "<div class='statName'>";
                                    echo "Min:";
                                echo "</div>";
                                echo "<div class='stats'>";
                                    echo $resultPlayerArray[$i]->getMin();
                                echo "</div>";
                            echo "</div>";  
                        echo "</div>";        
                        
                    echo "</div>";
                }
            }
        }
    ?>

</body>
</html>


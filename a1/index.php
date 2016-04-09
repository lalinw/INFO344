<html>
<head>
    <meta charset="UTF-8">
    <title>Document</title>
</head>
<body>

<img src="NBA_logo.png" alt="NBA" height="150" width="">
<form>
    <input type="text" name="search">
    <input type="submit" value="Submit">
</form>
<br>
<?php
    require 'classPlayer.php';
    //use require instead of include; stops if the class does not exist
    
    
    //connecting to the DB in RDS
    $conn = new PDO('mysql:host=nba-db.c6uuvnaayrmz.us-west-2.rds.amazonaws.com:3306;dbname=nbadb', 'info344user', '344password'); 
    $conn->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
    //check connection

    if(isset($_REQUEST['search'])) {
        $playerSearch = $_REQUEST['search']; //will throw error when load, because no input yet
        
        echo '<br> You searched <b>'.$playerSearch.'</b><br><br>';

        $sql = "SELECT * 
            FROM player_stats 
            WHERE Name LIKE '%".$playerSearch."%'";
            
        $result = $conn->query($sql)->fetchAll(PDO::FETCH_ASSOC);

        //create object from query    
        if (count($result) <= 0 ){
            echo "No results to display";
        } else {
            $resultPlayerArray = array();   //array of Player objects
            for($i = 0; $i < count($result); $i++) {
                //Player($name, $team, $ppg, $THptm, $reb, $ast, $stl, $blk, $to)
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
                echo $resultPlayerArray[$i]->getName();
                echo "<br>";
                echo $resultPlayerArray[$i]->getTeam();
                echo "<br>";
                echo "PPG: ".$resultPlayerArray[$i]->getPpg()."/ Assist: ".$resultPlayerArray[$i]->getAst();
                echo "<br><br>";
            }
        }
    }
    

?>
    
</body>
</html>


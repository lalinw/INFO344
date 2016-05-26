http://stackoverflow.com/questions/1678214/javascript-how-do-i-create-jsonp

<?php
	$callback = $_GET['callback'];
    if(isset($_GET['search']) && $_GET['search'] != "") {
        $playerSearch = $_REQUEST['search'];
         //connecting to the DB in RDS
        $conn = new PDO('mysql:host=nba-db.c6uuvnaayrmz.us-west-2.rds.amazonaws.com:3306;dbname=nbadb', 'info344user', '344password'); 
        //check connection
        $conn->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);

        $sql = "SELECT TOP 1 * 
            FROM player_stats 
            WHERE Name like " .$playerSearch;
        $result = $conn->query($sql)->fetchAll(PDO::FETCH_ASSOC);   //query the db

        echo $callback . "(" . json_encode($result) . ")";

    }
?>

JSON P 
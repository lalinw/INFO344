<?php
	// $callback = $_GET['search'];
    if(isset($_GET['search']) && $_GET['search'] != "") {
        $playerSearch = $_REQUEST['search'];
         //connecting to the DB in RDS
        $conn = new PDO('mysql:host=nba-db.c6uuvnaayrmz.us-west-2.rds.amazonaws.com:3306;dbname=nbadb', 'info344user', '344password'); 
        //check connection
        $conn->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
        $sql = "SELECT * 
            FROM player_stats 
            WHERE Name LIKE "."'".$playerSearch."' LIMIT 1";
        $result = $conn->query($sql)->fetchAll(PDO::FETCH_ASSOC);   //query the db
        header('Content-Type: application/json;');
        echo json_encode($result);
    }
?>

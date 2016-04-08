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
    //echo "hello world in php";
    
    
    //connecting to the DB in RDS
    $conn = new PDO('mysql:host=nba-db.c6uuvnaayrmz.us-west-2.rds.amazonaws.com:3306;dbname=nbadb', 'info344user', '344password'); 
    $conn->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
    //check connection

    if(isset($_REQUEST['search'])) {
        $playerSearch = $_REQUEST['search']; //will throw error when load, because no input yet
        
        echo '<br> You have searched '.$playerSearch.'<br>';
        //the query part? 
        //$stmt = $conn->query("SELECT * FROM player_stats WHERE Name = 'Stephen Curry'")->fetchAll(PDO::FETCH_ASSOC);

        $sql = "
            SELECT Name 
            FROM player_stats 
            WHERE Name LIKE '%".$playerSearch."%'";
        $result = $conn->query($sql)->fetchAll(PDO::FETCH_ASSOC);

        //show query    
        for($i = 0; $i < count($result); $i++) {
            echo $result[$i]['Name']."<br>";
        }
        if (count($result) <= 0 ){
            echo "No results to display";
        }
    }
    

?>
    
</body>
</html>


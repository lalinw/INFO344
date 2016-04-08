<html>
<head>
    <meta charset="UTF-8">
    <title>Document</title>
</head>
<body>
   
<?php 
    echo "hello world in php";
    
    $conn = new PDO('mysql:host=nba-db.c6uuvnaayrmz.us-west-2.rds.amazonaws.com:3306;dbname=nbadb', 'info344user', '344password'); 
    $conn->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);

    $stmt = $conn->query('SELECT * FROM player_stats')->fetchAll(PDO::FETCH_ASSOC);;

    var_dump($stmt);
?>
    
</body>
</html>


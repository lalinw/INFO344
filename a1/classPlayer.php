<?php
    class Player {
    //php, default is public
    
        function Player($name, $team, $ppg, $m3pt, $reb, $ast, $stl, $blk, $turnover) {
            $this->name = $name;
            $this->team = $team;
            $this->ppg = $ppg; 
            $this->m3pt = $m3pt;
            $this->reb = $reb;
            $this->ast = $ast;
            $this->stl = $stl;
            $this->blk = $blk;
            $this->turnover = $turnover;
        }
        
        public function getName() {
            return $this->name;     
        }
        
        public function getTeam() {
            return $this->team;     
        }

        public function getPpg() {
            return $this->ppg;     
        }
        
        public function getm3pt() {
            return $this->m3pt;     
        }
        
        public function getreb() {
            return $this->reb;     
        }
        
        public function getAst() {
            return $this->ast;     
        }
        
        public function getStl() {
            return $this->stl;     
        }
        
        public function getBlk() {
            return $this->blk;     
        }
        
        public function getTurnover() {
            return $this->turnover;     
        }
        
        public function getPlayer() {
            return $this->name." ".$this->team;
        }
    }


?>
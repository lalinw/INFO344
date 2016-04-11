<?php
    class Player {
    //php, default is public
    
        function Player($name, $team, $ppg, $ast, $stl, $blk, $turnover, $gp, $min, $pt3, $fg, $ft, $reb) {
            $this->name = $name;
            $this->team = $team;
            $this->ppg = $ppg; 
            $this->ast = $ast;
            $this->stl = $stl;
            $this->blk = $blk;
            $this->turnover = $turnover;
            $this->gp = $gp;
            $this->min = $min;
            $this->pt3 = $pt3;  //array attempt/made/percentages
            $this->fg = $fg;    //array
            $this->ft = $ft;    //array
            $this->reb = $reb;  //array offense/defense/total
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
        
        public function getGp() {
            return $this->gp;     
        }
        
        public function getMin() {
            return $this->min;     
        }
        
        public function getPt3() {
            return $this->pt3;  //array 
        }
        
        public function getFg() {
            return $this->fg;    //array
        }
        
        public function getFt() {
            return $this->ft;    //array
        }
        
        public function getReb() {
            return $this->reb;  //array
        }
            
        public function getPlayer() {
            return $this->name." ".$this->team;
        }
    }


?>
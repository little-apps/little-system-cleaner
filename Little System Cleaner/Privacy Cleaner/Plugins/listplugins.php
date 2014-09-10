<?php

//$dirname = basename( dirname( __FILE__ ) );
$dirname = 'Privacy Cleaner Plugins';

foreach (glob("*.xml") as $filename) {
	$file_path = $dirname . '\\' . $filename;
	
	echo '  File "/oname='.$file_path.'" "'.$file_path.'"' . PHP_EOL;
}
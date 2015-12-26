getURL:resize(coord,zoom);

function resize(coord, zoom){
	var quadKey = getQuadKey(coord,zoom);
	var parentQuadKey = quadKey.Trim(1)
	var tileXYZ = getTileXY(parentQuadKey);
	var image1 = "./"+tileXYZ[2]+"/"+tileXYZ[0]+"/"+tileXYZ[1];
	var image2 = increaseResolution(image1);
	
	var quad = quadKey.Trim(quadKey.Length-1);
	var smallImage = clip(image2,quad);
	return smallImage;	
}

function getQuadKey(coord,zoom){
	return quadKey;
}
function getTileXY(quadKey){
	return tileXYZ; 
}
function clip(image2,quad){
	switch(quad){
		case 0:
			return upperLeft;
			break;
		case 1:
			return upperRight;
			break;
		case 2:
			return lowerLeft;
			break;
		case 3:
			return lowerRight;
			break;
	}
}
function increaseResolution(image1){
	var factor = 2;
	var image2 = image1*factor;
	return image2;
}
rem desktopIcons
rem "C:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe" "C:\data\DesktopIcons\DesktopIcons\DesktopIcons.csproj"
rem "C:\data\DesktopIcons\DesktopIcons\bin\Debug\DesktopIcons.exe"

rem "C:\Program Files\MongoDB\Server\3.2\bin\mongod.exe"
rem "C:\Program Files\MongoDB\Server\3.2\bin\mongo.exe"








rem download
rem "C:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe" "C:\data\HousePricesDownload\HousePricesDownload\HousePricesDownload.csproj"
rem "C:\data\HousePricesDownload\HousePricesDownload\bin\Debug\HousePricesDownload.exe"

rem cleanData
rem "C:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe" "C:\data\HousePricesDownload\cleandata\cleanData.csproj"
rem "C:\data\HousePricesDownload\cleanData\bin\Debug\cleanData.exe"
rem db.prop.createIndex( { quad: 1 } )


rem tile
rem "C:\Program Files (x86)\MSBuild\12.0\Bin\MSBuild.exe" "C:\data\HousePricesDownload\globalmaptiles\GlobalMapTiles.csproj"
rem "C:\data\HousePricesDownload\GlobalMapTiles\bin\Debug\GlobalMapTiles.exe"

rem db.prop.find({nearMin:{$exists:true}}).count()
rem db.search.aggregate([{$group:{_id:null,total:{$sum:"$count"}}}])
rem db.search.find({size:{$eq:0.01},count:{$gte:900}).count()
rem "C:\Program Files\MongoDB\Server\3.2\bin\mongoexport" -d test -c prop | "C:\Program Files\MongoDB\Server\3.2\bin\mongoimport" -d p -c prop --drop
mongoexport -d test -c prop | mongoimport -d p -c prop --drop
mongoexport -d prop2 -c prop1 | mongoimport -d prop2Temp -c prop2 --drop
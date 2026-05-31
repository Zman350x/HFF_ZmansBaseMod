debug:
	msbuild -restore ZmanBaseMod.sln /property:Configuration="Debug"

release:
	msbuild -restore ZmanBaseMod.sln /property:Configuration="Release"

run:
	-pkill Human
	cp build/bin/output/ZmanBaseMod.dll ~/.steam/steam/steamapps/common/"Human Fall Flat"/BepInEx/plugins/ZmanBaseMod.dll
	(steam steam://rungameid/477160 &)

.PHONY: clean

clean:
	rm -rf build/obj
	rm -rf build/bin

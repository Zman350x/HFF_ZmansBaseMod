# Goals


# Code Snippets
```cs
private Traverse SetResolution;


            //Allow the user to change the game resolution from the in-game shell
            SetResolution = Traverse.Create(MenuSystem.instance.GetMenu<VideoMenu>()).Method("ForceResolution", 0, 0, false);
            Shell.RegisterCommand("res", (string txt) =>
            {
                string[] a = txt.Trim().Split(' ');
                StartCoroutine(SetResolution.GetValue<System.Collections.IEnumerator>(int.Parse(a[0]),
                                                                                      int.Parse(a[1]),
                                                                                      bool.Parse(a[2])));
            }, "Set Resolution");
```

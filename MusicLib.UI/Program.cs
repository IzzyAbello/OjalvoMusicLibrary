using MusicLib.Cli;

// Punto de entrada de 'ojalvomusic'. Por ahora la vista no existe.
//
// TODO(avalonia): cuando exista la ventana, pasarla así:
//   return new Controller(args, runView: StartAvalonia).Execute();
return new Controller(args).Execute();

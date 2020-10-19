using Thuja;
using Thuja.Widgets;

namespace FileManager
{
    internal class Program
    {
        /// <summary>
        /// Точка входа.
        /// </summary>
        private static void Main()
        {
            var help = new StackContainer()
                .Add(new Label("        `7MM\"\"\"YMM `7MMM.     ,MMF'"))
                .Add(new Label("          MM    `7   MMMb    dPMM"))
                .Add(new Label("          MM   d     M YM   ,M MM"))
                .Add(new Label("          MM\"\"MM     M  Mb  M' MM"))
                .Add(new Label("          MM   Y     M  YM.P'  MM"))
                .Add(new Label("          MM         M  `YM'   MM"))
                .Add(new Label("        .JMML.     .JML. `'  .JMML."))
                .Add(new Label("Настоятельно рекомендуется нажать F1.   "))
                .Add(new Label("           Выбор файлов:                ") {CurrentStyle = Style.Active})
                .Add(new Label("!Не забывайте выбирать файлы через Ins! "))
                .Add(new Label("                                        "))
                .Add(new Label("Tab - перейти к списку выбранных файлов "))
                .Add(new Label("Ins или Пробел-отменить выбор (в списке)"))
                .Add(new Label("             Команды:                   ") {CurrentStyle = Style.Active});
            foreach (var command in Actions.Commands)
            {
                help.Add(new Label(command));
            }

            var manager = new FileManager(80, 24);
            manager.AttachActions();
            manager.ChangeDir(null);

            var root = new BaseContainer()
                .AddFocused(manager.RootContainer)
                .Add(new RelativePosition(40, 1, -1)
                    .Add(help));

            var loop = new MainLoop(root);
            loop.Start();
        }
    }
}
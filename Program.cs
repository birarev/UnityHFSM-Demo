using System.Text;
using UnityHFSM;

Console.OutputEncoding = Encoding.UTF8;
var fsm = new StateMachine<string>();
int sequence = 0;
bool startRequested = false;
int workUnits = 0;

void Log(string message) => Console.WriteLine($"{++sequence:00}. {message}");
void LogTransition(string phase, TransitionBase<string> transition) =>
    Log($"{transition.from} → {transition.to}: {phase}; активно: {fsm.ActiveStateName}");

foreach (string name in new[] { "Idle", "Working", "Completed" })
    fsm.AddState(name, new StateBase<string>(needsExitTime: false));

// StateChanged is raised by the original FSM after its AfterTransition callback.
fsm.StateChanged += state => Log($"FSM.StateChanged: {state.name}");

var decorator = new TransitionDecorator<string>(
    beforeShouldTransition: t => LogTransition("до проверки условия", t),
    afterShouldTransition: t => LogTransition("после проверки условия", t),
    beforeOnTransition: t => LogTransition("до выполнения перехода / BeforeTransition", t),
    afterOnTransition: t => LogTransition("после BeforeTransition (смена состояния ещё впереди)", t),
    beforeAfterTransition: t => LogTransition("после смены состояния, до AfterTransition", t),
    afterAfterTransition: t => LogTransition("после выполнения перехода / AfterTransition", t));

var start = new Transition<string>(
    "Idle", "Working",
    condition: t =>
    {
        LogTransition($"condition: startRequested = {startRequested}", t);
        return startRequested;
    },
    onTransition: t => LogTransition("оригинальный Transition.onTransition", t),
    afterTransition: t => LogTransition("оригинальный Transition.afterTransition", t));

var finish = new Transition<string>(
    "Working", "Completed",
    condition: t =>
    {
        bool ready = workUnits >= 2;
        LogTransition($"condition: workUnits = {workUnits}, результат = {ready}", t);
        return ready;
    },
    onTransition: t => LogTransition("оригинальный Transition.onTransition", t),
    afterTransition: t => LogTransition("оригинальный Transition.afterTransition", t));

// Decorate returns real DecoratedTransition<string> wrappers around the originals.
DecoratedTransition<string> decoratedStart = decorator.Decorate(start);
DecoratedTransition<string> decoratedFinish = decorator.Decorate(finish);
fsm.AddTransition(decoratedStart);
fsm.AddTransition(decoratedFinish);
fsm.SetStartState("Idle");
fsm.Init();

void Tick(string description, string expectedState)
{
    Log($"--- {description} ---");
    fsm.OnLogic();
    Log($"Активное состояние: {fsm.ActiveStateName}");
    if (fsm.ActiveStateName != expectedState)
        throw new InvalidOperationException($"Ожидалось состояние {expectedState}.");
}

Tick("Ожидание команды", "Idle");
startRequested = true;
Tick("Команда запуска", "Working");
workUnits++;
Tick("Выполнена 1 из 2 единиц работы", "Working");
workUnits++;
Tick("Выполнены 2 из 2 единиц работы", "Completed");
Tick("Завершённый автомат: исходящих переходов нет", "Completed");
Log("Проверка сценария пройдена: Idle → Working → Completed.");

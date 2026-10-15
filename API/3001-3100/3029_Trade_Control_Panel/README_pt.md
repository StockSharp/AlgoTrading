# Estratégia Painel de Controle de Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Painel de Controle de Trading** porta o painel de negociação manual do script MQL5 original para a API de alto nível do StockSharp. A classe expõe métodos auxiliares que replicam cada botão do painel: alternadores de presets de volume, ações de compra/venda de mercado, fechamento da posição atual, reversão da exposição e uma rotina dedicada de ponto de equilíbrio. Ordens de stop-loss e take-profit de proteção podem ser geradas automaticamente em torno do preço médio de entrada, espelhando os recursos de segurança do consultor especialista fonte.

Em vez de desenhar controles de gráfico, a implementação do StockSharp fornece uma interface fortemente tipada que pode ser chamada a partir de código de UI, scripts ou fluxos de trabalho automatizados. A estratégia rastreia os presets de volume selecionados, arredonda volumes para o passo de exchange mais próximo e emite ordens de mercado/stop/limite através dos helpers integrados de `Strategy` como `BuyMarket`, `SellMarket`, `SellStop` e `BuyLimit`.

## Parâmetros
- **VolumeList** – presets de volume separados por ponto e vírgula que se comportam como as caixas de seleção originais. Apenas os primeiros nove valores são usados para manter compatibilidade com o layout MQL. Espaços em branco são ignorados e números inválidos são pulados.
- **CurrentVolume** – volume agregado com base nos presets atualmente alternados. O setter arredonda o valor usando `Security.VolumeStep` (quando disponível) ou duas casas decimais (lotes estilo forex). Você também pode definir este parâmetro manualmente ao integrar com uma UI externa.
- **BreakEvenSteps** – número de passos de preço adicionados ao preço de entrada ao mover o stop de proteção para o ponto de equilíbrio através de `ApplyBreakEven()`. Se o instrumento não tiver `PriceStep`, o valor é tratado como um offset de preço direto.
- **StopLossSteps** – distância inicial de stop-loss expressa em passos de preço. Um valor de zero desabilita os stops automáticos quando uma posição é aberta ou alterada.
- **TakeProfitSteps** – distância inicial de take-profit em passos de preço. Funciona da mesma forma que o parâmetro de stop-loss.

## Controles manuais
Todas as ações em tempo de execução são expostas através de métodos públicos para que o aplicativo host possa vinculá-los a botões, teclas de atalho ou scripts:

- `ToggleVolumeSelection(int index)` – imita as caixas de seleção de presets adicionando ou removendo um volume da quantidade agregada. Índices inválidos lançam exceção para evitar erros silenciosos.
- `ResetVolumeSelection()` – limpa cada preset e redefine `CurrentVolume` para zero.
- `ExecuteBuy()` / `ExecuteSell()` – enviam ordens de mercado usando o volume atual. Ambos os métodos retornam `false` quando nenhum volume está selecionado.
- `CloseAllPositions()` – envia uma ordem de mercado oposta ao tamanho da posição atual (`BuyMarket` para vendidos, `SellMarket` para comprados).
- `ReversePosition()` – fecha a posição existente e imediatamente abre uma nova na direção oposta usando o volume agregado, exatamente como o botão "Reverse" no painel MQL.
- `ApplyBreakEven()` – recalcula o stop de proteção como `preço médio de entrada ± BreakEvenSteps * PriceStep` e coloca uma nova ordem de stop (`SellStop` para comprados, `BuyStop` para vendidos). Retorna `true` apenas quando a estratégia mantém uma posição aberta e um offset maior que zero é fornecido.

Sempre que o tamanho da posição muda, `OnPositionChanged` reconstrói as ordens de proteção. Primeiro cancela o par anterior de stop/alvo, depois os recria usando o último preço médio de entrada e os offsets configurados. Fechar a posição (manualmente ou por execuções de stop/alvo) remove todas as ordens de proteção ativas para evitar instruções órfãs na exchange.

## Fluxo de trabalho de uso
1. Configure os presets de volume desejados em **VolumeList** (por exemplo `0.05; 0.10; 0.25; 0.50; 1.00`).
2. Alterne um ou mais presets com `ToggleVolumeSelection`. O parâmetro `CurrentVolume` mostra o valor acumulado após o arredondamento.
3. Chame `ExecuteBuy` ou `ExecuteSell` para entrar no mercado. Se **StopLossSteps** ou **TakeProfitSteps** forem maiores que zero, a estratégia colocará automaticamente ordens `SellStop`/`BuyStop` e `SellLimit`/`BuyLimit` relativas ao preço médio de entrada.
4. Use `ApplyBreakEven` quando o preço se mover a seu favor para arrastar o stop acima (para comprados) ou abaixo (para vendidos) da entrada pelo offset configurado.
5. `CloseAllPositions` sai do mercado imediatamente, enquanto `ReversePosition` fecha e inverte a exposição reutilizando o tamanho de lote atualmente selecionado.
6. `ResetVolumeSelection` prepara o painel para a próxima negociação limpando todos os presets.

## Notas e recomendações
- A lógica de ponto de equilíbrio e proteção depende de `PositionAvgPrice` e do `Security.PriceStep` atual. Certifique-se de que os metadados do instrumento estejam preenchidos antes de iniciar a estratégia.
- `StartProtection()` é chamado durante `OnStarted` para que o mecanismo de proteção integrado possa rastrear as ordens de stop/alvo que esta estratégia registra.
- Os métodos auxiliares são wrappers síncronos em torno dos helpers de ordem do StockSharp. Exchanges ou adaptadores que requerem confirmação assíncrona devem aguardar eventos de ordem antes de emitir o próximo comando se o sequenciamento estrito for necessário.
- A classe pode ser embutida em painéis personalizados WPF/WinForms, serviços REST ou ferramentas de console mapeando eventos de UI para os métodos expostos.

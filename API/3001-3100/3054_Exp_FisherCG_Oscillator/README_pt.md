# Estratégia Exp FisherCG Oscilador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o consultor especialista **Exp_FisherCGOscillator** do MetaTrader 5 para a API de alto nível do StockSharp. Ela recria o oscilador Fisher Center of Gravity e sua linha de gatilho, avalia sinais em uma barra histórica configurável e reproduz o fluxo de stop/take original com ordens do StockSharp e auxiliares de risco.

## Como funciona

1. **Pipeline de indicadores** – cada vela concluída é passada pelo oscilador Fisher CG: os preços medianos alimentam um loop de centro de gravidade, os valores são normalizados sobre as últimas `Length` barras, e uma transformação de Fisher produz a linha do oscilador. A linha de gatilho é simplesmente o oscilador atrasado por uma barra.
2. **Extração de sinais** – a estratégia inspeciona duas leituras históricas definidas por `SignalBar`. Abre uma posição comprada quando o valor mais antigo do oscilador (`SignalBar + 1`) está acima de seu gatilho enquanto o valor mais recente (`SignalBar`) cruza de volta acima do gatilho, sinalizando uma virada de alta. Posições vendidas espelham essa lógica no lado baixista.
3. **Tratamento de saídas** – saídas compradas ocorrem assim que o oscilador mais antigo cai abaixo de seu gatilho, enquanto saídas vendidas são acionadas quando sobe acima do gatilho, correspondendo aos sinalizadores de fechamento imediato do EA. Entradas opostas fecham a posição ativa antes de reverter.
4. **Processamento barra a barra** – tudo é executado em velas concluídas de `CandleType`; nenhum trade intrabar é gerado, garantindo backtests deterministas e correspondendo ao gate de "nova barra" do EA.

## Gestão de risco e dimensionamento de posição

- **Stops/alvos** – `StopLossPoints` e `TakeProfitPoints` são expressos em passos do instrumento e traduzidos em distâncias de preço absolutas via `Security.PriceStep`.
- **Controle de volume** – `SizingMode = FixedVolume` envia o `FixedVolume` constante. `SizingMode = PortfolioShare` converte `DepositShare` do valor atual do portfólio em contratos usando o último fechamento e `VolumeStep`.
- **Posição única** – a estratégia sempre nivela antes de entrar no lado oposto, evitando posições hedgeadas simultâneas.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `CandleType` | Período subscrito para velas e cálculos de indicadores. |
| `Length` | Período do oscilador Fisher CG (também usado para a janela de normalização). |
| `SignalBar` | Número de velas fechadas para trás usadas para leitura de sinais; `1` corresponde ao padrão do EA. |
| `AllowLongEntry` / `AllowShortEntry` | Alternar entradas compradas/vendidas. |
| `AllowLongExit` / `AllowShortExit` | Alternar saídas automáticas para posições compradas/vendidas. |
| `StopLossPoints` / `TakeProfitPoints` | Distâncias de stop de proteção e alvo em passos de preço. Definir como `0` para desabilitar. |
| `FixedVolume` | Volume usado no modo de dimensionamento fixo. |
| `DepositShare` | Fração do portfólio alocada por trade no modo `PortfolioShare`. |
| `SizingMode` | Escolhe entre volume fixo e dimensionamento baseado em participação. |

## Notas de uso

- Alinhe `CandleType` e `SignalBar` com o período usado pelo indicador original (H8 e deslocamento de barra de 1 por padrão).
- Permita um breve período de aquecimento para que o oscilador tenha histórico suficiente para se formar; a estratégia ignora trades até que o indicador esteja completamente inicializado.
- Stops e alvos operam no fechamento do ローソク足. Ajuste os valores de pontos para corresponder ao tamanho do tick do seu instrumento.
- Quando o dimensionamento `PortfolioShare` for selecionado, certifique-se de que a valoração do portfólio esteja disponível; caso contrário, a estratégia voltará ao volume fixo.

## Diferenças vs. EA original

- As ordens são enviadas como ordens de mercado sem o parâmetro de deslizamento `Deviation_`; o StockSharp lida com a execução com suas próprias configurações de deslizamento.
- O gerenciamento monetário é simplificado para dois modos de dimensionamento (`FixedVolume` e `PortfolioShare`). As opções de percentual de perda do EA são intencionalmente omitidas.
- Carimbos de data/hora de ordens pendentes (`UpSignalTime`/`DnSignalTime`) não são usados. Os sinais são executados imediatamente na vela processada.

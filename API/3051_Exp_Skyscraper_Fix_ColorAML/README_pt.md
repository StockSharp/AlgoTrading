# Estratégia Exp Skyscraper Fix ColorAML
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia recria o consultor especialista do MetaTrader 5 **Exp_Skyscraper_Fix_ColorAML** dentro do framework StockSharp.
Ela combina dois geradores de sinais independentes:

1. **Skyscraper Fix** – um canal baseado em ATR que pinta regimes de alta ou baixa dependendo da direção das bandas adaptativas.
2. **ColorAML** – um oscilador adaptativo de nível de mercado que compara intervalos fractais locais para detectar fases de
   expansão ou contração.

A implementação MQL original gerenciava dois magic numbers separados e podia manter posições hedgeadas simultaneamente. As
estratégias do StockSharp operam com uma posição líquida, portanto sinais conflitantes simplesmente se compensam e a última
entrada define o exposure. O README destaca essas diferenças para que os usuários alinhem as expectativas ao fazer backtesting
ou operar com a variante convertida.

## Parâmetros
### Módulo Skyscraper Fix
- **SkyscraperCandleType** – período utilizado para construir o indicador Skyscraper Fix. Padrão: velas `4h`.
- **SkyscraperEnableLongEntry / SkyscraperEnableShortEntry** – permitem ao módulo abrir posições compradas ou vendidas.
- **SkyscraperEnableLongExit / SkyscraperEnableShortExit** – permitem ao módulo fechar trades abertos na direção correspondente.
- **SkyscraperLength** – número de amostras de ATR utilizadas para determinar o tamanho do degrau. Padrão: `10` barras.
- **SkyscraperMultiplier** – coeficiente aplicado ao passo baseado em ATR. Padrão: `0.9`.
- **SkyscraperPercentage** – deslocamento percentual opcional aplicado à linha média (0 desativa o deslocamento).
- **SkyscraperMode** – escolhe entre a construção do canal baseada em High/Low ou em Close.
- **SkyscraperSignalBar** – número de velas concluídas a examinar ao ler o buffer de cores. Os valores devem ser pelo menos `1`.
- **SkyscraperVolume** – volume da ordem de mercado solicitada em cada entrada.
- **SkyscraperStopLoss / SkyscraperTakeProfit** – distâncias de proteção expressas em passos de preço.

### Módulo ColorAML
- **ColorAmlCandleType** – período utilizado pelo oscilador ColorAML. Padrão: velas `4h`.
- **ColorAmlEnableLongEntry / ColorAmlEnableShortEntry** – habilitam novas entradas compradas ou vendidas.
- **ColorAmlEnableLongExit / ColorAmlEnableShortExit** – habilitam ordens de fechamento para a respectiva direção.
- **ColorAmlFractal** – comprimento do intervalo fractal usado para construir os níveis adaptativos. Padrão: `6` barras.
- **ColorAmlLag** – parâmetro de lag que controla a suavização exponencial. Padrão: `7`.
- **ColorAmlSignalBar** – número de velas concluídas a inspecionar no buffer de cores.
- **ColorAmlVolume** – volume da ordem para entradas impulsionadas pelo ColorAML.
- **ColorAmlStopLoss / ColorAmlTakeProfit** – distâncias de proteção em passos de preço.

## Lógica de trading
A estratégia subscreve as séries de velas solicitadas para cada módulo e avalia apenas as velas concluídas. Ambos os indicadores
estão implementados em C# seguindo as definições matemáticas do código MQL original:

- **Skyscraper Fix** calcula um canal similar ao SuperTrend. Quando o buffer de cores muda para **teal (0)**, o módulo fecha
  qualquer exposure vendido (se permitido) e, quando a cor anterior era diferente, prepara uma entrada comprada. Quando o buffer
  muda para **firebrick (1)**, fecha comprados e agenda uma entrada vendida.
- **ColorAML** compara intervalos fractais para construir uma linha de nível adaptativo. A cor `2` sinaliza expansão de alta,
  fechando vendidos e abrindo comprados opcionalmente. A cor `0` sinaliza contração de baixa, fechando comprados e abrindo
  vendidos opcionalmente. O neutro `1` mantém a postura atual.

Cada entrada usa ordens de mercado dimensionadas como `VolumeConfigurado + |posição atual|`. Isso garante que uma ordem de
reversão feche simultaneamente o exposure oposto e estabeleça a nova posição quando o hedge não está disponível.

## Gestão de risco
`StartProtection()` é ativado no início. Sempre que um módulo abre uma nova posição, a estratégia armazena o preço de entrada e
calcula os níveis de stop-loss e take-profit usando as configurações específicas do módulo. Velas subsequentes acionam saídas se
seu máximo ou mínimo perfurar os limites configurados. Definir as distâncias como zero desativa a lógica de proteção.

## Notas de implementação
- Os cálculos do Skyscraper Fix e do ColorAML foram portados diretamente e executam em buffers internos de velas. Não é
  necessário adicionar indicadores externos manualmente à estratégia.
- O StockSharp mantém uma única posição líquida por estratégia. Como resultado, trades simultâneos comprados e vendidos do EA
  original são compensados. Usuários que dependiam de hedge devem estar cientes dessa diferença.
- Apenas velas concluídas são processadas. `SignalBar` deve ser pelo menos `1`; a avaliação intrabar (tick a tick) não é
  reproduzida.
- Os stops são aplicados monitorando os extremos das velas em vez de ordens do lado do servidor, o que corresponde ao
  comportamento do framework convertido.

## Uso
1. Vincule a estratégia ao ativo e ao portfólio desejados.
2. Configure os parâmetros para ambos os módulos, alinhando os tipos de velas com os dados disponíveis.
3. Inicie a estratégia. Ela subscreverá automaticamente as velas necessárias, calculará as cores dos indicadores e colocará
   ordens de mercado de acordo com os sinais do módulo.
4. Monitore o log ou os gráficos para observar mudanças de regime, eventos de gestão de risco manual e trades executados.

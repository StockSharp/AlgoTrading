# Estratégia de Cruzamento RSI EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia RSI EA replica o consultor especializado "RSI EA" do MetaTrader 5. Ela observa o Índice de Força Relativa (RSI) na série de velas selecionada e reage quando o momentum cruza níveis configuráveis de sobrecompra ou sobrevenda. A conversão mantém as ideias de stop-loss, take-profit, trailing-stop e gestão automática do dinheiro do sistema original enquanto as adapta à API de estratégia de alto nível do StockSharp.

## Lógica da Estratégia

### Indicadores
- **RSI** com um período configurável aplicado ao tipo de vela escolhido.

### Critérios de Entrada
- **Comprado**: o RSI cruza **acima** de `RsiBuyLevel` (valor anterior abaixo do limiar, valor atual acima do limiar) e o trading comprado está habilitado.
- **Vendido**: o RSI cruza **abaixo** de `RsiSellLevel` (valor anterior acima do limiar, valor atual abaixo do limiar) e o trading vendido está habilitado.

Apenas uma posição líquida é mantida. Se a estratégia já estiver no mercado, nenhuma posição de hedge adicional é aberta.

### Critérios de Saída
- **Saída por sinal**: quando `CloseBySignal` está habilitado, o cruzamento RSI oposto fecha imediatamente a posição ativa.
- **Stop protetor**: quando `StopLoss` é maior que zero, a estratégia monitora a distância do preço a partir do preço médio de entrada e sai assim que a perda atingir o valor especificado.
- **Take-profit**: quando `TakeProfit` é maior que zero, a posição é fechada assim que a distância alvo é atingida.
- **Trailing stop**: quando `TrailingStop` é maior que zero, o nível do stop segue o preço. Para posições compradas, o stop é elevado para `Close - TrailingStop` assim que o preço avança pelo menos `TrailingStop` a partir do stop atual; vendidos se comportam simetricamente.

### Dimensionamento de Posição
- Quando `UseAutoVolume` é `true`, o volume é calculado a partir do patrimônio da conta e risco: `Volume = Equity * RiskPercent / (100 * stopDistance)`, onde `stopDistance` usa `StopLoss` se disponível e caso contrário `TrailingStop`. Se nenhuma distância de proteção for definida, a estratégia recorre ao volume manual.
- Quando `UseAutoVolume` é `false`, o parâmetro fixo `ManualVolume` é usado para cada ordem.

## Parâmetros
- `CandleType`: série de velas usada para cálculo do indicador (padrão: período de 1 minuto).
- `RsiPeriod`: número de barras na janela de cálculo do RSI (padrão: 14).
- `RsiBuyLevel`: limite de sobrevenda que aciona entradas compradas e saídas vendidas (padrão: 30).
- `RsiSellLevel`: limite de sobrecompra que aciona entradas vendidas e saídas compradas (padrão: 70).
- `EnableLong`: habilitar ou desabilitar trades comprados (padrão: true).
- `EnableShort`: habilitar ou desabilitar trades vendidos (padrão: true).
- `CloseBySignal`: fechar posições quando o RSI cruzar o limiar oposto (padrão: true).
- `StopLoss`: distância do stop-loss em unidades de preço (padrão: 0, desabilitado).
- `TakeProfit`: distância do take-profit em unidades de preço (padrão: 0, desabilitado).
- `TrailingStop`: distância do trailing stop em unidades de preço (padrão: 0, desabilitado).
- `UseAutoVolume`: ativar dimensionamento de posição baseado em risco (padrão: true).
- `RiskPercent`: porcentagem do patrimônio a arriscar quando o dimensionamento automático está ativo (padrão: 10).
- `ManualVolume`: tamanho de ordem fixo quando o dimensionamento automático está desabilitado (padrão: 0.1).

## Notas de Implementação
- A implementação do StockSharp usa o fluxo de trabalho de alto nível `SubscribeCandles(...).Bind(...)`, permitindo que o indicador RSI entregue valores diretamente à estratégia sem gerenciamento manual de buffers.
- A estratégia redefine todos os níveis de proteção quando a posição retorna a zero para evitar valores obsoletos de stop ou take-profit.
- A lógica de trailing espelha o código MQL: o stop só é ajustado após o preço percorrer mais do dobro da distância de trailing além do nível de stop atual, evitando o aperto prematuro.
- Como as estratégias do StockSharp operam em um ambiente de netting, não é possível manter posições compradas e vendidas simultâneas como no EA de hedge original. Em vez disso, a estratégia aguarda o fechamento da posição atual antes de abrir na direção oposta.
- O dimensionamento automático requer que `StopLoss` ou `TrailingStop` sejam definidos; caso contrário, o volume manual é usado porque a distância de risco é desconhecida.

## Configuração Padrão
- Período: velas de 1 minuto.
- RSI: período 14, níveis 30/70.
- Gestão do dinheiro: volume automático habilitado, risco de patrimônio de 10%, volume de fallback manual 0.1.
- Controles de risco: sem stop-loss, take-profit ou trailing stop por padrão (devem ser configurados para trading ao vivo).

## Dicas de Uso
- Configure `CandleType` para corresponder ao instrumento e ao horizonte temporal que pretende operar; a estratégia funciona em qualquer intervalo suportado pelas velas do StockSharp.
- Forneça distâncias de stop-loss ou trailing-stop realistas antes de habilitar o dimensionamento automático para que o cálculo de risco use valores significativos.
- Combine a estratégia com `StartProtection()` (já chamado no código) para permitir que o framework gerencie desconexões inesperadas ou posições órfãs.
- Monitore execuções e ajuste os níveis de RSI ao aplicar a estratégia a diferentes mercados, pois os limiares ideais podem variar.

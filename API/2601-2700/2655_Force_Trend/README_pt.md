# Estratégia de Tendência Forçada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Conversão do consultor especializado MetaTrader 5 **Exp_ForceTrend.mq5** localizado em `MQL/18817`.
- Usa o oscilador ForceTrend proprietário para detectar transições entre momentum de alta e de baixa.
- Implementa a lógica com a API de alto nível do StockSharp, dependendo de assinaturas de candles e indicadores integrados em vez de acesso direto a séries.

## Indicador ForceTrend
- O indicador olha para trás `Length` candles e mede a distância entre a máxima mais alta e a mínima mais baixa dentro dessa janela.
- O preço médio do candle atual é normalizado dentro desse intervalo e suavizado duas vezes:
  - O primeiro estágio produz um valor `force` intermediário com coeficientes `0.66` e `0.67`.
  - O segundo estágio aplica uma transformação logarítmica combinada com suavização de meia-vida para obter o valor final de ForceTrend.
- Valores acima de zero são tratados como de alta (originalmente renderizados em azul) e valores abaixo de zero são de baixa (renderizados em magenta).

## Parâmetros
- `Length` – tamanho da janela de lookback do ForceTrend; deve permanecer positivo.
- `SignalBar` – quantos candles concluídos o sinal é deslocado. `0` reage à barra fechada mais recente, `1` imita a configuração padrão do MT5 esperando uma barra extra, e valores maiores atrasam ainda mais a execução.
- `EnableLongEntry` – se desabilitado, a estratégia não abrirá posições compradas em transições de alta.
- `EnableShortEntry` – se desabilitado, a estratégia não abrirá posições vendidas em transições de baixa.
- `EnableLongExit` – controla se sinais de alta podem fechar posições vendidas existentes.
- `EnableShortExit` – controla se sinais de baixa podem fechar posições compradas existentes.
- `CandleType` – período dos candles usados para cálculos do indicador.

## Regras de negociação
1. A saída do ForceTrend é convertida em uma direção discreta (`+1`, `0`, `-1`).
2. As direções são armazenadas em um histórico de comprimento fixo para que a estratégia possa comparar a barra no offset `SignalBar` com a barra imediatamente anterior.
3. Um sinal de alta (`direction > 0`) aciona:
   - Fechar qualquer posição vendida aberta se `EnableShortExit` for `true`.
   - Abrir ou reverter para uma posição comprada (ordem a mercado de tamanho `Volume + |Position|`) quando a direção anterior não era de alta e `EnableLongEntry` é `true`.
4. Um sinal de baixa (`direction < 0`) aciona as ações simétricas para posições compradas quando `EnableLongExit`/`EnableShortEntry` estão habilitados.
5. Leituras neutras do ForceTrend herdam a última direção conhecida para que o sistema não oscile entre estados neutros.
6. As ordens são enviadas apenas quando a estratégia está completamente formada, online e a negociação é permitida pelo runtime do StockSharp.

## Notas de implementação
- Os candles são recebidos através de `SubscribeCandles(CandleType)`; o processamento do indicador é realizado no callback `ProcessCandle`.
- Os preços mais altos e mais baixos são obtidos via indicadores `Highest` e `Lowest` do StockSharp, garantindo que não seja necessário gerenciamento manual de buffers nem operações LINQ.
- O histórico de direção é armazenado em um pequeno array fixo dimensionado de acordo com `SignalBar` para reproduzir o comportamento MT5 original sem recriar coleções para cada tick.
- As reversões de posição usam uma única ordem a mercado com volume igual à soma da exposição desejada e a posição absoluta atual, emulando os helpers `BuyPositionOpen`/`SellPositionOpen` da versão MQL.
- Os parâmetros de gestão monetária do consultor especializado (dimensionamento de lotes, stop-loss e take-profit em pontos) são omitidos intencionalmente; a estratégia StockSharp depende do `Volume` configurado pelo usuário e módulos de proteção externos opcionais.
- Os interruptores booleanos espelham as entradas MT5 (`BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose`).

## Dicas de uso
- Configurar a propriedade `Volume` antes de iniciar a estratégia para controlar o tamanho da ordem.
- Escolher um tipo de candle que corresponda ao período usado durante os testes no MT5 (padrão: candles de quatro horas).
- Combinar com componentes de risco/proteção do StockSharp se a automação de stop-loss ou take-profit for necessária.

## Arquivos
- Implementação da estratégia: `CS/ForceTrendStrategy.cs`
- Arquivos MQL originais: `MQL/18817/mql5/Experts/Exp_ForceTrend.mq5` e `MQL/18817/mql5/Indicators/ForceTrend.mq5`

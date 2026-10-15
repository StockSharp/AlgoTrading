# Lucro difícil
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Hard Profit é uma versão StockSharp do MetaTrader 4 consultor especialista `hardprofit.mq4`. A estratégia tenta capturar fugas
após um movimento de exaustão, quando o fechamento termina no extremo da vela e um filtro de tendência suavizado confirma a direção.
A porta reconstrói os modos originais de gerenciamento de dinheiro, realização de lucros encenado e gerenciamento de parada usando StockSharp's
API de alto nível.

## Lógica estratégica
### Configuração de intervalo
* A estratégia monitora as velas finalizadas a partir do período de tempo configurado e acompanha a máxima mais alta e a mínima mais baixa do
barras `Breakout Period` anteriores (a vela atual é excluída, emulando a chamada `iHighest`/`iLowest` com um deslocamento de 1).
* Os preços medianos alimentam uma média móvel suavizada com período `Trend Period`. A inclinação da média móvel (valor atual menos
valor anterior) é o filtro direcional usado pelo EA original.

### Regras de entrada
* **Inserções longas** são consideradas quando:
  * A vela fecha em sua máxima e quebra acima da máxima anterior.
  * A inclinação da média móvel suavizada é positiva.
  * Não há posição aberta e o limite de negociação por barra não foi atingido.
  * O spread atual (melhor venda menos melhor lance) está abaixo do limite de `Max Spread (pips)` quando ambos os lados estão disponíveis.
  * As negociações longas não são desativadas por `Only Short`.
* **Entradas curtas** refletem as condições acima: fechamento na mínima, rompimento abaixo da mínima da faixa anterior, inclinação da tendência negativa,
filtro de propagação respeitado e `Only Long` desativado.

### Gerenciamento de saída
* Um stop-loss fixo (`Stop Loss (pips)`) e um take-profit opcional (`Take Profit (pips)`) definem o envelope protetor externo.
* Quando o lucro não realizado atinge `Break-even (pips)` o stop é movido para o preço de entrada. Depois de `Trailing Activation (pips)` o
stop salta à frente na distância stop-loss, garantindo lucro assim como a implementação MetaTrader.
* Duas saídas parciais reciclam as percentagens originais:
  * `Partial TP1 (pips)` fecha `Partial Ratio 1 (%)` da posição ativa.
  * `Partial TP2 (pips)` fecha `Partial Ratio 2 (%)` da posição restante.
A lógica funciona no volume da posição atual, de modo que a segunda parcial é dimensionada com o que resta após o primeiro corte.
* Stops e alvos reagem aos extremos intrabarras: uma negociação longa sairá quando a mínima da vela ultrapassar o stop ou quando a máxima
atinge a meta de lucro; as negociações curtas usam as condições simétricas.

### Gestão de dinheiro
Cinco modos de dimensionamento imitam o comportamento MetaTrader ao contabilizar os dados do portfólio StockSharp:
1. **Fixo** – usa `Fixed Volume` em todas as entradas.
2. **Geométrico** – dimensiona com a raiz quadrada do valor do portfólio (`0.1 * sqrt(balance / 1000) * Geometrical Factor`).
3. **Proporcional** – aloca uma fração do patrimônio livre em relação ao último fechamento (`equity * Risk Percent / (price * 1000)`).
4. **Smart** – parte da alocação proporcional e reduz o tamanho quando mais de uma perda consecutiva é detectada pelo
usando o divisor `Decrease Factor`.
5. **TSSF** – recria a lógica Triggered Smart Safe-Factor. A vitória média, a perda média e a taxa de vitória são calculadas a partir do valor mais
resultados obtidos recentemente `Last Trades`. A métrica derivada alterna entre os divisores `TSSF Ratio` configurados ou faz fallback
para um mínimo de 0,1 lote quando as condições se deteriorarem. Todos os volumes são normalizados para `VolumeStep`, `MinVolume` do instrumento,
e `MaxVolume` restrições.

## Parâmetros
* **Período de Breakout** – número de velas finalizadas usadas para calcular os máximos e mínimos do rompimento.
* **Período de Tendência** – duração da média móvel suavizada aplicada ao preço mediano.
* **Apenas Curto / Somente Longo** – alternadores direcionais que desativam o lado oposto.
* **Max Trades Per Bar** – proteção de negociação por barra (0 desativa o limite).
* **Stop Loss (pips)** – distância inicial do stop loss; definido como 0 para desabilitar.
* **Break-even (pips)** – limite de lucro que move o stop para o nível de entrada.
* **Ativação de Trailing (pips)** – limite de lucro que avança o stop no tamanho do stop original.
* **TP1 Parcial (pips)** / **Relação Parcial 1 (%)** – distância e percentual da primeira saída parcial.
* **TP2 Parcial (pips)** / **Relação Parcial 2 (%)** – distância e percentual para a segunda saída parcial.
* **Take Profit (pips)** – meta de lucro final; 0 desativa o alvo difícil.
* **Max Spread (pips)** – spread máximo permitido no momento da entrada.
* **Gerenciamento de dinheiro** – seleciona o modo de dimensionamento (Fixo, Geométrico, Proporcional, Inteligente, TSSF).
* **Volume Fixo** – volume base quando o modo de gerenciamento de dinheiro é Fixo.
* **Fator Geométrico** – multiplicador utilizado pela fórmula de dimensionamento geométrico.
* **Percentual de Risco** – percentual de capital livre utilizado pelo dimensionamento proporcional, inteligente e TSSF.
* **Últimas negociações** – número de negociações realizadas recentemente armazenadas para dimensionamento adaptativo.
* **Fator de Diminuição** – divisor aplicado ao modo inteligente quando ocorrem perdas consecutivas.
* **TSSF Trigger 1/2/3 e TSSF Ratio 1/2/3** – limites e divisores para as transições de métricas TSSF.
* **Tipo de vela** – período principal que impulsiona atualizações de indicadores e avaliação de sinal.

## Notas adicionais
* Os valores do pip são derivados da etapa do preço do título; símbolos FX de cinco dígitos mapeiam automaticamente um pip para 10 pontos.
* As saídas parciais não zeram o contador de negociações por barra, replicando o comportamento MetaTrader de contar apenas novas entradas.
* As estatísticas de gestão de dinheiro são construídas a partir de diferenças de PnL realizadas, de modo que a história se torna significativa assim que as primeiras negociações
feche no ambiente StockSharp.
* Se os melhores dados de compra/venda não estiverem disponíveis, o filtro de spread será efetivamente desativado, correspondendo ao comportamento do EA original quando
o corretor relatou um spread zero.

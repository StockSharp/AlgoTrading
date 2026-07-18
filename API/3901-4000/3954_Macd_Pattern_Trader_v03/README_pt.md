# Macd Pattern Trader v03 (porta StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Macd Pattern Trader v03 é uma estratégia StockSharp de alto nível convertida do consultor especialista MetaTrader 4 *MacdPatternTraderv03*. O robô original procura na linha principal MACD uma formação de reversão de três picos e aplica regras parciais de realização de lucro com base em médias móveis. Esta porta C# preserva a lógica padrão ao usar StockSharp assinaturas, indicadores e auxiliares de pedido.

A estratégia foi projetada para configurações de exaustão de tendências em pares de FX líquidos, mas pode ser aplicada a qualquer instrumento que exponha uma curva MACD suave. O período padrão é de velas de 30 minutos, correspondendo ao consultor original, e o tamanho da negociação padrão é um contrato (ou lote equivalente em termos de StockSharp).

## Indicadores e fluxo de dados
* **MACD (EMA rápida 5, EMA lenta 13, Sinal 1)** — indicador principal usado para detectar a estrutura triplo topo/triplo fundo. A linha de sinal não é usada; a estratégia depende apenas da linha principal MACD.
* **EMA(7) e EMA(21)** — médias curtas e médias usadas durante o gerenciamento de posições.
* **SMA(98) e EMA(365)** — filtros lentos que formam o gatilho de expansão.

A implementação assina o tipo de vela configurado e vincula os indicadores por meio de `Bind` / `BindEx`. Apenas velas finalizadas são processadas para evitar atuação em dados incompletos.

## Regras de entrada
### Configuração curta
1. Arme a configuração quando a linha principal MACD ultrapassar o nível **Ativação superior** (padrão 0,0030).
2. Registre o primeiro pico assim que MACD imprimir um máximo local acima dos valores anteriores e anteriores e, em seguida, cair abaixo do **Limite Superior** (padrão 0,0045).
3. Registre o segundo pico se MACD retornar acima do limite, atingir um máximo local mais alto e cair novamente abaixo do limite.
4. Confirme o padrão quando ocorrer um terceiro rollover com MACD permanecendo abaixo do limite por três barras consecutivas e o último máximo local for inferior ao anterior.
5. Se não existir nenhuma posição longa, nivele qualquer exposição longa restante e abra uma posição curta com o volume configurado.

### Configuração longa
1. Arme a configuração quando a linha principal MACD cair abaixo do nível **Ativação inferior** (padrão -0,0030).
2. Registre o primeiro vale assim que MACD imprimir um mínimo local abaixo dos dois valores anteriores e, em seguida, subir acima do **Limite Inferior** (padrão -0,0045).
3. Registre o segundo vale se MACD cair abaixo do limite, atingir um mínimo inferior e subir acima do limite novamente.
4. Confirme o padrão de alta quando um terceiro aumento for observado com MACD permanecendo acima do limite por três velas e o último mínimo for superior ao anterior.
5. Achate qualquer exposição curta restante e compre o volume configurado.

A lógica espelha os sinalizadores `stops`, `stops1` e `aop_ok*` aninhados no arquivo MQ4 original, incluindo redefinições sempre que MACD ultrapassa a banda de ativação.

## Gestão comercial
* **Escalonamento horizontal** — quando o lucro não realizado (calculado como `(Close − Entry) * Position`) excede `ProfitThreshold` (padrão 5 unidades de preço), a estratégia aplica duas saídas escalonadas:
  * Estágio 1 (longo): o fechamento da vela anterior deve ficar acima de EMA(21). A estratégia vende um terço da posição longa inicial. Para posições vendidas, o requisito é o fechamento anterior abaixo de EMA(21) e um terço do volume vendido inicial é recomprado.
  * Estágio 2 (longo): a máxima da vela anterior deve ultrapassar a média de SMA(98) e EMA(365). Metade da posição longa original está fechada. Os shorts refletem isso com o mínimo anterior caindo abaixo do filtro médio.
* **Posição residual** — o que resta após a sequência de escalonamento não ser gerenciada por esta porta, correspondendo à origem EA.
* **Ordens de risco** — a versão MetaTrader colocou ordens de stop-loss e take-profit com base em máximos e mínimos contínuos. Como StockSharp gerencia ordens de proteção de maneira diferente, esta porta não anexa automaticamente paradas/alvos. Os usuários podem combinar a estratégia com `StartProtection()` ou um módulo de risco externo, se necessário.

## Parâmetros
| Nome | Padrão | Descrição |
| ---- | ------- | ----------- |
| `Volume` | 1 | Tamanho da negociação enviado em cada entrada. |
| `CandleType` | Período de 30 minutos | Série de velas usada para cálculos de indicadores. |
| `FastEmaLength` / `SlowEmaLength` | 5/13 | MACD períodos EMA rápidos e lentos. |
| `UpperThreshold` / `LowerThreshold` | 0,0045 / −0,0045 | Faixa de exaustão onde acontecem as confirmações de padrões. |
| `UpperActivation` / `LowerActivation` | 0,0030 / −0,0030 | Banda externa que arma as configurações de baixa/alta. |
| `EmaOneLength` / `EmaTwoLength` | 21/07 | EMAs auxiliares para visualização e lógica de escalonamento. |
| `SmaLength` | 98 | Lento SMA usado junto com EMA(365) durante as saídas do estágio dois. |
| `EmaFourLength` | 365 | EMA de longo prazo usado durante as saídas do estágio dois. |
| `ProfitThreshold` | 5 | PnL mínimo não realizado (preço * unidades de volume) necessário antes da expansão. |

## Notas práticas
* Certifique-se de que o adaptador intermediário suporte redução parcial de posição. O EA original fechou 1/3 e 1/2 porções; esta porta replica as mesmas frações usando ordens de mercado.
* Como as ordens de proteção não são anexadas automaticamente, considere ativar `StartProtection()` ou adicionar regras de risco personalizadas se precisar de interrupções bruscas.
* O limite de lucro é expresso em unidades de preço bruto * volume. Ajuste-o de acordo com o tamanho do pip ou valor do tick do instrumento para corresponder à suposição de “5 unidades monetárias” do código MQ4 original.
* A estratégia espera uma dinâmica MACD suave; ruído excessivo ou instrumentos ilíquidos podem impedir o disparo da lógica de três picos.

## Diferenças da versão MQ4
* Usa ligações de indicadores StockSharp em vez de chamadas `iMACD` repetidas.
* O cálculo do lucro não realizado depende de `Position` e `PositionAvgPrice`, o que significa que as regras de arredondamento do corretor podem ser diferentes das `OrderProfit()` de MetaTrader.
* As ordens stop-loss e take-profit não são geradas automaticamente; ferramentas manuais de risco devem ser adicionadas, se necessário.
* O parâmetro MQ4 `sum_bars_bup` não está presente porque não foi utilizado na origem original.

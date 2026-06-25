# Estratégia de Flechas e Curvas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um port em C# do assessor especialista (EA) "Arrows and Curves" do MetaTrader 5. Ela replica a lógica baseada em indicadores dentro do StockSharp usando a API de alto nível. O sistema opera um único símbolo e reage aos sinais do canal personalizado gerados pelo indicador Arrows and Curves. Apenas uma posição pode estar ativa por vez, e cada novo sinal abre uma nova operação ou fecha uma existente.

## Lógica da estratégia
- As velas do período configurável são transmitidas via `SubscribeCandles`. A rotina de processamento trabalha apenas com velas finalizadas para espelhar o comportamento do EA nas aberturas de novas barras.
- O canal Arrows and Curves é reconstruído dentro da estratégia: o algoritmo escaneia o máximo mais alto e o mínimo mais baixo para a janela de retrocesso `SSP`, deslocada pelo offset `Relay` exatamente como o indicador MT5. A partir desses valores são derivados dois envelopes (`Channel %` para a banda exterior e `Channel Stop %` para a banda interior).
- As variáveis de estado do indicador (`uptrend` e `uptrend2`) são atualizadas exatamente na mesma ordem que no código MQL original. Sempre que a vela anterior produz uma flecha Sell, a estratégia prepara uma entrada comprada; e sempre que produz uma flecha Buy, prepara uma entrada vendida. Isso espelha o comportamento do EA onde os sinais são lidos com índice 1 na barra seguinte.
- Quando não há posição aberta, o sinal armazenado da barra anterior é usado para abrir uma ordem a mercado na direção oposta à flecha (flecha Sell → operação de compra, flecha Buy → operação de venda).
- Quando já existe uma posição e um sinal oposto aparece, a posição atual é fechada, mas uma posição inversa não é aberta imediatamente, correspondendo à fonte MT5 onde o fechamento ocorre primeiro e as entradas são avaliadas novamente na barra seguinte.

## Gestão de risco
- As distâncias de stop loss e take profit são definidas em pips e convertidas em offsets de preço absolutos usando o `PriceStep` do instrumento. Para instrumentos cotados com 3 ou 5 casas decimais, a conversão multiplica o passo por dez, reproduzindo os ajustes de pip do EA.
- A funcionalidade de trailing stop espelha o EA: uma vez que o lucro flutuante supera `Trailing Stop + Trailing Step`, o stop de proteção é arrastado pela distância configurada respeitando o passo mínimo.
- Os níveis de proteção são verificados em cada vela completada usando o máximo/mínimo da vela para aproximar os gatilhos intrábarra.
- O dimensionamento da posição pode ser fixado via parâmetro `Volume`. Quando `Volume` é zero, a estratégia deriva uma quantidade dinâmica arriscando `Risk %` do valor do portfólio contra a distância de stop loss configurada.

## Parâmetros
- `Volume`: tamanho de ordem fixo. Definir como zero para habilitar o dimensionamento baseado em risco.
- `Risk %`: percentual do valor do portfólio a arriscar quando o volume é zero.
- `Stop Loss (pips)`: distância do stop de proteção em pips.
- `Take Profit (pips)`: distância do alvo de lucro em pips.
- `Trailing Stop (pips)`: distância do trailing stop em pips; definir como zero para desabilitar.
- `Trailing Step (pips)`: movimento adicional mínimo necessário antes que o trailing stop seja deslocado novamente.
- `SSP`: número de velas usadas para calcular o intervalo do canal.
- `Channel %`: percentual do envelope exterior, idêntico à configuração do MT5.
- `Channel Stop %`: percentual do envelope interior usado para alternar o estado secundário.
- `Relay`: deslocamento aplicado ao cálculo do canal.
- `Candle Type`: período ou tipo de vela que alimenta o indicador.

## Notas de implementação
- A estratégia armazena apenas a quantidade mínima de máximos, mínimos e fechamentos históricos exigida pelo indicador (`SSP + Relay + 5` barras).
- Todos os comentários e métodos auxiliares são escritos em inglês para atender às diretrizes do repositório.
- Ao contrário do MT5, as ordens de stop loss e take profit são simuladas em dados de velas, portanto as execuções intrábarra podem diferir do EA original. Todo o restante segue as mesmas regras de decisão, tornando o port fiel ao script fonte.

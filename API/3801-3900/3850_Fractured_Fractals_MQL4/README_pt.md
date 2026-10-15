# Estratégia Fraturada Fractals (MT4)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Porta C# detalhada do clássico MetaTrader 4 consultor especialista `MQL/7696/Fractured_fractals.mq4`. A estratégia está atenta aos recém-confirmados
Williams níveis fractais, ordens de parada de quebra de filas e riscos de trilhas usando as oscilações fractais anteriores. O dimensionamento da posição segue o
lógica original de risco por negociação com a redução de volume adaptativa "DecreaseFactor" após rebaixamentos.

## Detalhes

- **Fonte**: convertido de `MQL/7696/Fractured_fractals.mq4`.
- **Regime de Mercado**: Continuação do rompimento, funciona em qualquer instrumento que forme estruturas fractais confiáveis.
- **Tipos de ordem**: usa ordens de stop para entradas e ordens de stop de proteção para saídas.
- **Dimensionamento de posição**: modelo de risco percentual controlado por `MaximumRiskPercent` com amortecimento de sequência de perdas por meio de `DecreaseFactor`.
- **Parâmetros padrão**:
  - `MaximumRiskPercent` = 2%
  - `DecreaseFactor` = 3
  - `CandleType` = período de 1 hora
- **Indicadores principais**: Detecção fractal nativa de cinco barras Williams implementada na estratégia.
- **Tipo de estratégia**: rompimento longo/curto simétrico com trailing stops baseados em fractal.

## Lógica da estratégia

### Detecção fractal

- Mantém uma janela contínua de cinco máximos e mínimos de velas para reproduzir os buffers `iFractals` de MetaTrader.
- Um novo fractal ascendente é confirmado quando o máximo médio excede os dois máximos circundantes de cada lado; um fractal descendente requer o
médio baixo para ser o mais baixo na sequência de cinco compassos.
- Quando um novo fractal aparece, ele é armazenado junto com os três valores anteriores, espelhando `cfu`, `pfu` de EA e
Buffers de estilo `pfu.1` para comparações posteriores e cálculos finais.

### Configuração de entrada

- As negociações longas exigem que o fractal ascendente mais recente exceda o anterior e o fractal descendente mais recente para definir um piso de risco.
A estratégia então coloca um stop de compra ligeiramente acima do fractal (compensação de spread) com um stop de proteção abaixo do oposto.
para baixo fractal.
- As negociações curtas refletem a lógica: um fractal inferior inferior combinado com um fractal superior superior gera um stop de venda e uma proteção.
pare acima do fractal ascendente mais spread.
- Apenas uma ordem pendente por direção é permitida. Se a estrutura fractal invalidar o padrão - por exemplo, o último fractal não
excede o anterior – a ordem pendente é cancelada imediatamente.

### Parar o gerenciamento

- Uma vez posicionado, o bot segue o stop de proteção usando o fractal anterior no lado de entrada, subtraindo/adicionando a corrente
espalhar. O stop apenas se move a favor da negociação.
- Quando a direção da posição muda ou fecha, a ordem de parada não utilizada é cancelada para evitar exposição obsoleta.

### Gestão de risco

- `CalculateOrderVolume` replica o cálculo de risco por negociação de EA: o tamanho da posição é a proporção entre a permissão de risco monetário e
a distância entre os níveis de entrada e parada.
- A avaliação da conta prefere `Portfolio.CurrentValue`; se não estiver disponível, a rotina volta para a propriedade `Volume` da estratégia
multiplicado pelo preço.
- Após duas ou mais negociações consecutivas perdidas, o volume é reduzido em `losses / DecreaseFactor`, emulando o MetaTrader
Comportamento `DecreaseFactor`.

### Acompanhamento do ciclo comercial

- `OnOwnTradeReceived` agrega preenchimentos em ciclos de negociação, rastreia PnL flutuante e atualiza a sequência de perdas assim que o volume retorna
para plano. Isso mantém a lógica de risco alinhada com o especialista MT4 onde `HistoryTotal` foi usado para analisar resultados anteriores.

## Notas de uso

1. Anexe a estratégia a qualquer par título/carteira e escolha uma resolução `CandleType` apropriada que corresponda ao original
EA configuração.
2. Garanta que as cotações de nível 1 estejam disponíveis – a estimativa do spread depende da melhor oferta/venda; se não estiver disponível, a estratégia volta a
`PriceStep`.
3. As ordens de parada pressupõem que a corretora oferece suporte a paradas no servidor. Substitua o registro `BuyStop`/`SellStop` por ordens de mercado se
exigido pelo seu adaptador.
4. Como o processamento ocorre no fechamento da vela, os sinais fractais intrabarras só são acionados no final de cada barra, reproduzindo o
avaliação barra por barra do consultor especialista.

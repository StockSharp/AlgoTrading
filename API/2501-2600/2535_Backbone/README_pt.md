# Estratégia Backbone
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o comportamento central do consultor especialista original **Backbone** do MQL5 usando a API de alto nível do StockSharp. Ela alterna entre ciclos de trading de alta e baixa, escala em posições de acordo com uma fração de risco e protege as operações abertas com alvos fixos junto com um trailing stop.

## Ideia central

1. **Detecção de direção inicial** – a estratégia rastreia a máxima mais alta e a mínima mais baixa após o início. Um movimento maior que a distância do trailing stop longe de qualquer extremo define qual lado operará primeiro.
2. **Ciclos direcionais** – após um ciclo começar, o algoritmo opera apenas nessa direção até que todas as posições sejam fechadas. Quando a última posição sai, ele imediatamente inverte e se prepara para o ciclo oposto.
3. **Escalonamento baseado em risco** – cada entrada adicional usa um volume dinâmico derivado do capital da conta, a fração `MaxRisk`, o limite configurado `MaxTrades` e a distância do stop-loss. Isso imita a função de dimensionamento de lotes do EA original.
4. **Saídas protetoras** – cada entrada recalcula um nível de stop-loss e take-profit em torno do preço médio ponderado por volume do ciclo atual. Um trailing stop ajusta o stop protetor sempre que o lucro não realizado excede a distância de trailing configurada.

## Parâmetros

| Parâmetro | Valores padrão | Descrição |
|-----------|---------|-------------|
| `MaxRisk` | 0.5 | Fração do capital da conta disponível para todas as posições na direção atual. |
| `MaxTrades` | 10 | Número máximo de entradas sequenciais por ciclo direcional. |
| `TakeProfitPips` | 170 | Distância (em pips) entre a média de entrada e o alvo de take-profit. |
| `StopLossPips` | 40 | Distância (em pips) entre a média de entrada e o stop protetor. |
| `TrailingStopPips` | 300 | Distância (em pips) usada tanto para determinar a direção inicial quanto para seguir os lucros. |
| `CandleType` | Período de 5 minutos | Tipo de vela usado para avaliação de sinais. |

> **Definição de pip** – a estratégia ajusta automaticamente o tamanho do pip com base no instrumento `PriceStep`. Símbolos cotados com 3 ou 5 casas decimais usam um multiplicador de 10×, replicando o tratamento de pip original do MetaTrader.

## Lógica de trading

1. Aguardar uma vela finalizada. Pular o processamento enquanto a estratégia está aquecendo ou o trading está desabilitado.
2. Atualizar os preços extremos enquanto nenhuma direção foi escolhida ainda. Uma vez que a máxima rompe para cima (em mais de `TrailingStopPips`) o primeiro ciclo será vendido; se a mínima romper para baixo, o primeiro ciclo será comprado.
3. Enquanto o ciclo é comprado:
   - Adicionar uma nova entrada comprada quando (a) o ciclo anterior foi vendido e não há posições compradas abertas, ou (b) o ciclo anterior também foi comprado e o número de comprados abertos está abaixo de `MaxTrades`.
   - Sair de todo o ciclo comprado quando o take-profit ou stop-loss é atingido, ou quando o trailing stop eleva o nível protetor acima do stop atual.
4. Enquanto o ciclo é vendido, as mesmas regras se aplicam com condições invertidas.
5. Após um ciclo fechar, reiniciar seus contadores e aguardar a configuração oposta.

## Dimensionamento de posição

O tamanho de posição para cada nova entrada é calculado como:

```
qty = equity * fraction / (pipSize * stopLoss)
onde fraction = 1 / (MaxTrades / MaxRisk - openTrades)
```

A quantidade é então alinhada ao passo de volume do instrumento e limitada dentro dos limites mínimo/máximo de volume. Se o tamanho necessário cair abaixo do mínimo permitido, o mínimo é usado. Quando informações de capital não estão disponíveis, o volume de estratégia padrão atua como fallback.

## Gestão de saída

- **Stop-loss / take-profit** – recalculado sempre que uma nova ordem é adicionada para que todas as operações no ciclo atual compartilhem os mesmos níveis combinados baseados no preço médio de entrada.
- **Trailing stop** – para um ciclo comprado, o stop se move para `Close - TrailingStopPips * pipSize` assim que o lucro não realizado excede esse limiar. O trailing do lado vendido é tratado simetricamente.

## Notas e limitações

- O StockSharp executa operações em um ambiente de netagem, portanto cada ciclo direcional gerencia a posição combinada em vez de tickets individuais. A lógica alternada e a fórmula de risco reproduzem o comportamento original enquanto se adequam ao modelo de API.
- A estratégia depende de velas concluídas. Movimentos intrabar menores que o intervalo da vela não são avaliados.
- Garantir que o tipo de vela selecionado e o instrumento produzam dados suficientes para construir os extremos iniciais antes de esperar por operações.

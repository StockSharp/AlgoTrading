# Estratégia Improve MA & RSI com Cobertura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o expert original do MetaTrader "Improve" para o StockSharp usando a API de alto nível. Opera simultaneamente dois instrumentos: o símbolo principal selecionado para a estratégia e um símbolo de cobertura. A direção da operação é definida pela relação entre duas médias móveis suavizadas no instrumento principal e o índice de força relativa (RSI). A perna de cobertura espelha a direção da perna principal, criando uma exposição pareada que busca lucrar com movimentos de momentum sincronizados enquanto limita o risco de um único instrumento.

## Lógica da estratégia

- Calcular duas Médias Móveis Suavizadas (SMMA) no símbolo primário com períodos rápido e lento configuráveis.
- Calcular RSI nas mesmas velas e monitorar os limiares de sobrevenda/sobrecompra.
- Entrar **comprado** em ambos os instrumentos quando a SMMA lenta está acima da SMMA rápida e o RSI está em ou abaixo do limiar de sobrevenda.
- Entrar **vendido** em ambos os instrumentos quando a SMMA lenta está abaixo da SMMA rápida e o RSI está em ou acima do limiar de sobrecompra.
- As posições permanecem abertas até que o lucro aberto combinado de ambas as pernas exceda a meta monetária configurada, momento em que a estratégia liquida ambos os lados.

O algoritmo acompanha os preços de fechamento mais recentes de cada instrumento. O lucro combinado é estimado a partir da diferença entre o fechamento atual e o preço de entrada armazenado de cada perna. Como nenhum stop-loss é aplicado, as posições podem permanecer abertas por períodos prolongados quando o preço não atinge a meta de lucro.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| **Volume** | Quantidade de ordem para ambos os instrumentos, o primário e o de cobertura. |
| **Profit Target** | Meta monetária compartilhada por ambas as pernas; quando atingida, a estratégia fecha cada posição aberta. |
| **Hedge Security** | Instrumento secundário que é negociado junto com o instrumento principal. |
| **Fast MA** | Período da Média Móvel Suavizada rápida (padrão 8). |
| **Slow MA** | Período da Média Móvel Suavizada lenta (padrão 21). Deve ser maior que o período da MA rápida. |
| **RSI Period** | Comprimento usado para calcular o RSI (padrão 21). |
| **Oversold** | Nível RSI que aciona entradas compradas junto com a condição de MA (padrão 30). |
| **Overbought** | Nível RSI que aciona entradas vendidas junto com a condição de MA (padrão 70). |
| **Candle Type** | Período para cálculos; padrão velas de 1 hora, mas pode ser ajustado. |

## Indicadores

- **Média Móvel Suavizada (SMMA)** – usada duas vezes para definir os componentes de tendência rápida e lenta.
- **Índice de Força Relativa (RSI)** – determina condições de sobrevenda/sobrecompra para confirmação.

## Regras de entrada e saída

1. **Entrada comprada**
   - SMMA lenta &gt; SMMA rápida no símbolo primário.
   - RSI ≤ Sobrevenda.
   - Ambas as pernas são abertas com ordens a mercado na mesma direção (compra/compra).
2. **Entrada vendida**
   - SMMA lenta &lt; SMMA rápida no símbolo primário.
   - RSI ≥ Sobrecompra.
   - Ambas as pernas são abertas com ordens a mercado na mesma direção (venda/venda).
3. **Saída**
   - Quando `(lucro primário + lucro de cobertura) ≥ Profit Target`, a estratégia fecha ambas as posições usando ordens a mercado.
   - Nenhuma lógica adicional de stop-loss ou trailing é aplicada; o gerenciamento de risco deve ser adicionado externamente se necessário.

## Notas de uso

- Garantir que tanto o instrumento principal quanto o de cobertura estejam atribuídos antes de iniciar a estratégia; caso contrário, lançará uma exceção.
- A estimativa de lucro combinado depende dos preços de fechamento de velas. Derrapagem e diferenças de execução entre as duas pernas podem afetar o lucro real realizado.
- Como a estratégia abre ambas as pernas simultaneamente, é adequada para instrumentos correlacionados (por exemplo, pares de moedas ou futuros relacionados) onde se espera que se movam em conjunto.
- Considerar adicionar controles de risco em nível de portfólio ao negociar ao vivo, pois o algoritmo original usa apenas a meta de lucro virtual para saídas.

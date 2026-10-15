# Estratégia simples MACD EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Simple MACD EA é uma porta direta do clássico MetaTrader consultor especialista "Simple MACD EA". A abordagem usa duas médias móveis exponenciais (EMAs) para emular o histograma MACD e determinar a tendência dominante em velas de um minuto. As posições longas são abertas quando o EMA rápido (período 100) cruza acima do EMA lento (nível MACD definido pelo usuário). As posições curtas são abertas quando o EMA rápida cai abaixo do EMA lenta. Apenas uma posição é mantida por vez.

## Lógica de Gestão Comercial
- **Detecção de tendência:** A diferença entre o EMA de 100 períodos e o MACD EMA configurável define a direção da tendência atual (`+1`, `0`, `-1`). Uma reversão de negativo para positivo abre uma posição longa. Uma reversão de positivo para negativo abre uma posição curta.
- **Confirmação de impulso:** A estratégia monitora a diferença entre o MACD EMA e um EMA ligeiramente mais lento (`MACD level + 1`). Se a diferença diminuir em relação à negociação atual depois que o preço tiver movimentado pelo menos cinco pontos de lucro, a posição será fechada antecipadamente.
- **Proteção baseada em tempo:** Depois que uma negociação permanece aberta por um número definido de ciclos de avaliação pelo usuário, o sistema ativa uma parada suave que reduz a tolerância a movimentos adversos de preços em relação ao preço de entrada.
- **Saída móvel:** Depois que a negociação passa para o lucro e permanece ativa por ciclos suficientes, um trailing stop interno é acionado. O nível de stop segue o preço pelo número de pontos configurado e pode ser atualizado um número limitado de vezes. Se o limite for atingido, a posição será fechada.
- **Saída de reversão de tendência:** Quando o sinal de tendência muda na direção oposta enquanto o preço já está com cinco pontos de lucro, a posição é fechada imediatamente.

## Parâmetros
- **Tipo de vela** – Período usado para os cálculos EMA (padrão: velas de 1 minuto).
- **Volume** – Volume de pedidos para novas entradas.
- **MACD Nível** – comprimento EMA que define o componente lento MACD. Um EMA secundário com comprimento `MACD Level + 1` é derivado automaticamente.
- **Trailing Stop** – Distância em pontos para a saída final. Defina como zero para desativar.
- **Atualizações de Trailing** – Número máximo de ajustes de trailing stop por negociação.
- **Ciclos de espera** – Número de avaliações de velas a serem aguardadas antes que a parada suave adaptativa se torne ativa.

## Notas adicionais
- A estratégia sempre nivela a posição atual antes de reverter a direção.
- As informações da etapa de preço do título selecionado são usadas para converter distâncias baseadas em pontos em preços reais.
- A implementação depende da assinatura de vela de alto nível de StockSharp API e não enfileira buffers de indicadores personalizados.

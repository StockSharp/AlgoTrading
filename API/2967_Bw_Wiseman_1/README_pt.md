# Estratégia de Rompimento Bw WiseMan-1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port StockSharp do assessor especializado MetaTrader **Exp_BW-wiseMan-1**. Ele automatiza a lógica de rompimento WiseMan-1 de Bill Williams construída em torno do indicador Alligator. Os sinais são produzidos sempre que uma vela completada escapa das mandíbulas do Alligator e simultaneamente rompe os extremos de oscilação mais recentes. O modo contrário opcional troca os sinais para que a estratégia possa esmaecer os mesmos rompimentos.

## Ideia principal
- Calcular o Alligator de Bill Williams usando médias móveis suavizadas do preço mediano (alto + baixo) / 2.
- Deslocar as linhas de mandíbula, dentes e lábios para frente por deslocamentos configuráveis para corresponder à visualização do indicador original.
- Confirmar um rompimento apenas quando a vela atual se expande além dos máximos ou mínimos das últimas *N* barras, garantindo que o movimento seja mais forte que o ruído recente.
- Atrasar a execução pelo número selecionado de velas completadas para que o trader possa operar em sinais mais antigos, se desejado.

## Regras de trading
### Direção comprada
1. A barra deve terminar **abaixo** de todas as três linhas do Alligator (preço alto menor que mandíbula, dentes e lábios).
2. O preço de fechamento precisa estar na metade superior da vela, ou seja, acima da mediana da vela.
3. O mínimo da vela deve ser estritamente inferior aos mínimos das barras `Back` anteriores.
4. Quando o sinal se torna ativo após o atraso `SignalBar`:
   - Fechar qualquer vendido aberto se `Close Short` está habilitado.
   - Abrir uma nova posição comprada se `Enable Long` está habilitado e nenhuma posição está atualmente aberta.

### Direção vendida
1. A barra deve terminar **acima** de todas as três linhas do Alligator (preço baixo maior que mandíbula, dentes e lábios).
2. O preço de fechamento deve estar na metade inferior da vela, ou seja, abaixo da mediana da vela.
3. O máximo da vela tem que ser maior que os máximos das barras `Back` anteriores.
4. Quando o sinal se torna ativo:
   - Fechar qualquer comprado existente se `Close Long` está habilitado.
   - Abrir uma nova posição vendida se `Enable Short` está habilitado e não há posição atual.

### Modo contrário
Definir `Counter-Trend Mode` como **true** troca os sinais de compra e venda para que a estratégia tome operações contra a direção de rompimento do Alligator.

## Parâmetros
- **Candle Type** – período usado para construir velas e calcular todos os valores do indicador (padrão: 1 hora).
- **Counter-Trend Mode** – inverter a lógica de rompimento para operar contra a tendência primária (padrão: habilitado, seguindo o EA original).
- **Breakout Depth (`Back`)** – número de barras anteriores comparadas com o máximo/mínimo atual ao validar um rompimento (padrão: 2).
- **Jaw Length / Shift** – comprimento da média móvel suavizada e deslocamento para frente para a linha de mandíbula (padrões: 13 / 8).
- **Teeth Length / Shift** – comprimento da média móvel suavizada e deslocamento para frente para a linha de dentes (padrões: 8 / 5).
- **Lips Length / Shift** – comprimento da média móvel suavizada e deslocamento para frente para a linha de lábios (padrões: 5 / 3).
- **Signal Bar** – número de velas já terminadas para aguardar antes de executar um sinal detectado (padrão: 1).
- **Enable Long / Enable Short** – interruptores para abrir novas posições compradas ou vendidas.
- **Close Long / Close Short** – interruptores para fechar posições opostas quando o sinal dispara.

## Notas
- A estratégia depende exclusivamente de ordens a mercado e não define níveis rígidos de stop-loss ou take-profit. Qualquer saída é impulsionada pelo sinal oposto ou desabilitando o interruptor de fechamento relevante.
- Todos os cálculos são realizados em velas terminadas; dados parciais intrabar são ignorados para manter consistência com o especialista MetaTrader de origem.
- O volume é herdado das configurações de estratégia do StockSharp. Ajuste o volume base na configuração da plataforma se precisar de um tamanho de posição diferente.

# Estratégia do Sistema Oscilador Vortex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O Sistema Oscilador Vortex é uma portagem direta do expert advisor de MetaTrader 5 que depende do Oscilador Vortex para capturar mudanças bruscas entre o movimento direcional positivo e negativo. O oscilador é construído como a diferença entre a linha positiva do Vortex (VI+) e a linha negativa do Vortex (VI-) calculada na série de velas selecionada. Leituras profundamente negativas indicam que VI- domina VI+, enquanto valores fortemente positivos mostram a liderança de VI+. A estratégia interpreta esses extremos como zonas de inflexão potencial e reage com entradas de estilo reversão à média apoiadas por saídas impulsionadas pelo oscilador.

## Como a estratégia funciona
1. As velas são construídas usando o período configurado e alimentadas ao `VortexIndicator` integrado.
2. Uma vez que o indicador é formado, o valor do oscilador é derivado como `VI+ - VI-` em cada vela concluída.
3. O oscilador é comparado com limiares definidos pelo usuário:
   - Quando cai abaixo do limiar de compra, uma configuração longa é detectada.
   - Quando sobe acima do limiar de venda, uma configuração curta é detectada.
4. Filtros opcionais podem restringir sinais longos à zona entre o limiar de compra e um nível de stop-loss dedicado (e vice-versa para sinais curtos).
5. Sempre que um novo setup aparece, a estratégia fecha qualquer posição oposta e abre uma operação na direção do sinal com o volume configurado.
6. As posições abertas são monitoradas continuamente. Se o oscilador atingir os limites de stop-loss ou take-profit configurados, a posição é fechada imediatamente.

Esta sequência reproduz a lógica original do MetaTrader: as operações são avaliadas apenas em barras concluídas, ambas as direções são mutuamente exclusivas, e as regras protetoras baseadas no oscilador governam as saídas.

## Critérios de entrada
- **Entrada comprada**
  - Acionada quando o oscilador é menor ou igual ao limiar de compra.
  - Se a opção de stop-loss comprado estiver habilitada, o oscilador também deve permanecer acima do nível de stop-loss comprado.
  - Qualquer posição vendida ativa é fechada antes de abrir a operação comprada.
- **Entrada vendida**
  - Acionada quando o oscilador é maior ou igual ao limiar de venda.
  - Se a opção de stop-loss vendido estiver habilitada, o oscilador também deve permanecer abaixo do nível de stop-loss vendido.
  - Qualquer posição comprada ativa é fechada antes de abrir a operação vendida.
- Se o valor do oscilador estiver entre os limiares de compra e venda, todos os setups são cancelados e nenhuma alteração de posição ocorre.

## Critérios de saída
- **Posições compradas**
  - Fechar imediatamente quando o oscilador cruzar abaixo ou igualar o nível de stop-loss comprado (se habilitado).
  - Fechar imediatamente quando o oscilador subir até ou acima do nível de take-profit comprado (se habilitado).
- **Posições vendidas**
  - Fechar imediatamente quando o oscilador cruzar acima ou igualar o nível de stop-loss vendido (se habilitado).
  - Fechar imediatamente quando o oscilador cair até ou abaixo do nível de take-profit vendido (se habilitado).

As verificações de saída são realizadas após cada fechamento de vela, garantindo uma recriação fiel do loop de monitoramento do MT5.

## Parâmetros
- **Vortex Length** – período de retrocesso para o indicador Vortex (padrão 14).
- **Candle Type** – período usado para construir as velas fornecidas ao indicador.
- **Use Buy Stop Loss** – habilita o filtro de stop-loss baseado no oscilador e saída para operações compradas.
- **Use Buy Take Profit** – habilita a saída de take-profit baseada no oscilador para operações compradas.
- **Use Sell Stop Loss** – habilita o filtro de stop-loss baseado no oscilador e saída para operações vendidas.
- **Use Sell Take Profit** – habilita a saída de take-profit baseada no oscilador para operações vendidas.
- **Buy Threshold** – valor do oscilador que qualifica uma entrada comprada (padrão -0.75).
- **Buy Stop Loss Level** – valor do oscilador que fecha posições compradas quando a opção de stop-loss comprado está ativa (padrão -1.00).
- **Buy Take Profit Level** – valor do oscilador que fecha posições compradas quando a opção de take-profit comprado está ativa (padrão 0.00).
- **Sell Threshold** – valor do oscilador que qualifica uma entrada vendida (padrão 0.75).
- **Sell Stop Loss Level** – valor do oscilador que fecha posições vendidas quando a opção de stop-loss vendido está ativa (padrão 1.00).
- **Sell Take Profit Level** – valor do oscilador que fecha posições vendidas quando a opção de take-profit vendido está ativa (padrão 0.00).
- **Volume** – tamanho da operação usado para novas posições (padrão 0.1, correspondendo ao expert advisor original).

## Notas de implementação
- O processamento ocorre estritamente em velas concluídas para evitar duplicação de sinais dentro da mesma barra.
- Os limiares do oscilador podem ser otimizados graças aos intervalos fornecidos nos metadados de parâmetros.
- A estratégia inverte automaticamente as posições enviando uma ordem a mercado grande o suficiente para fechar o lado oposto e estabelecer a nova exposição.
- As funcionalidades de stop-loss e take-profit funcionam de forma independente; habilitar uma não requer a outra.

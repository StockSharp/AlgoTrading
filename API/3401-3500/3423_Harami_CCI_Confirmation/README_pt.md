# Harami CCI Confirmação
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Harami CCI Confirmação é uma porta StockSharp de alto nível do MetaTrader 5 consultor especialista `Expert_ABH_BH_CCI`. O EA original negocia os padrões de reversão Bullish Harami e Bearish Harami de duas velas. Antes de entrar em uma negociação, ele exige a confirmação de um oscilador do Commodity Channel Index (CCI) e mede o tamanho do corpo da vela em relação a uma média móvel para garantir que a vela maior realmente domine o intervalo. A conversão StockSharp mantém a mesma lógica de confirmação, processa apenas velas concluídas e usa o módulo de proteção integrado da plataforma para segurança do pedido.

## Lógica estratégica
### Detecção de padrões
* **Cálculo do corpo médio** – mantém uma média móvel dos corpos absolutos das velas nas últimas *N* barras (padrão 5). Isso reflete a classe auxiliar MetaTrader que suaviza o tamanho da vela e a referência de tendência.
* **Bullish Harami** – exige que a vela anterior seja de alta, a vela anterior seja de baixa com um corpo mais longo que a média e o corpo de alta permaneça dentro da faixa de baixa. O ponto médio da vela anterior também deve ficar abaixo da média móvel de fechamento, confirmando uma tendência de baixa.
* **Bearish Harami** – condições espelhadas: a vela anterior deve ser de baixa, a vela anterior deve ser de alta e longa, o corpo de baixa deve estar contido dentro da faixa de alta e o ponto médio precisa estar acima da média móvel próxima para confirmar uma tendência de alta.

### CCI confirmação
* **Filtro de entrada** – a estratégia verifica o valor CCI da vela concluída mais recentemente (turno 1). As negociações longas exigem que CCI esteja abaixo de `-EntryThreshold` (padrão 50), enquanto as negociações curtas exigem um valor acima de `+EntryThreshold`.
* **Banda de saída** – o histórico CCI é monitorado para cruzamentos de ±`ExitBand` (padrão 80). Quando o indicador sobe até `-ExitBand`, qualquer posição curta aberta é fechada. Quando cai abaixo de `+ExitBand`, a longa exposição existente é fechada. Isso reproduz os "votos" usados ​​pelo especialista MetaTrader para nivelar posições.

### Gestão comercial
* **Reversões** – se a configuração oposta de Harami for confirmada enquanto a estratégia já mantém uma posição, ela negociará volume suficiente para fechar a exposição existente e abrir a nova direção.
* **Proteção** – `StartProtection()` é ativado para que os usuários possam anexar configurações de stop-loss ou take-profit por meio da IU do StockSharp, se desejarem. Nenhuma parada fixa é aplicada por padrão para permanecer alinhado com a fonte EA, que dependia de configurações manuais de gerenciamento de dinheiro.

## Parâmetros
* **Volume de pedidos** – volume base enviado a cada entrada no mercado. O volume extra é adicionado automaticamente para fechar a posição oposta quando ocorre uma reversão.
* **CCI Período** – duração do oscilador do Commodity Channel Index.
* **Body Average** – número de velas históricas usadas para calcular a média do tamanho do corpo e preços de fechamento.
* **CCI Entrada** – valor absoluto mínimo CCI necessário para aceitar um sinal Harami.
* **CCI Banda de Saída** – magnitude da banda que define as regras de saída de cruzamento CCI.
* **Tipo de vela** – período usado para velas (padrão: período de 1 hora).

## Notas adicionais
* Todos os cálculos são executados em velas concluídas fornecidas por `SubscribeCandles`. Os sinais intrabar são intencionalmente ignorados para corresponder ao modelo de execução MetaTrader.
* A estratégia mantém um breve histórico deslizante de velas e valores CCI para avaliar as regras Harami sem recriar buffers completos de indicadores.
* Somente a implementação do C# é fornecida nesta pasta; não há versão Python para esta conversão.

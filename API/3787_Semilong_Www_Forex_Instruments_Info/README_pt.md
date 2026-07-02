# Estratégia de informações de instrumentos Forex semilong WWW
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica o comportamento do especialista "Semilong" MetaTrader. Ele monitora a distância entre o preço de oferta atual e dois preços de fechamento históricos separados por mudanças configuráveis. Quando o mercado atual é negociado suficientemente abaixo (ou acima) do fechamento mais antigo, enquanto o fechamento mais antigo também se afastou de uma referência ainda mais antiga, a estratégia abre uma posição longa (ou curta). O gerenciamento de posição reflete o script original com take-profit configurável, stop loss, trailing stop opcional e um módulo de lote automático que reduz o tamanho após perdas consecutivas.

## Geração de Sinal
- **Mudanças históricas** – `ShiftOne` seleciona quantas velas concluídas desde o primeiro fechamento de comparação, enquanto `ShiftTwo` adiciona um deslocamento extra para o segundo fechamento.
- **Filtros de desvio** – `MoveOnePoints` define a que distância o lance atual deve ser negociado desde o primeiro fechamento deslocado e `MoveTwoPoints` mede a distância entre os dois fechamentos deslocados.
- **Configuração longa** – Acionada quando o lance atual está pelo menos `MoveOnePoints` abaixo do primeiro fechamento deslocado e o primeiro fechamento deslocado está pelo menos `MoveTwoPoints` acima do segundo fechamento deslocado.
- **Configuração curta** – Acionado quando o lance atual está pelo menos `MoveOnePoints` acima do primeiro fechamento deslocado e o primeiro fechamento deslocado está pelo menos `MoveTwoPoints` abaixo do segundo fechamento deslocado.
- A estratégia espera por velas concluídas, ignora sinais quando as ordens já estão ativas e requer margem livre positiva antes de negociar.

## Gestão Comercial
- **Ordens de proteção iniciais** – Em vez de registrar ordens pendentes, a estratégia emula o comportamento original rastreando o preço de entrada e saindo do mercado assim que o movimento atingir:
  - `ProfitPoints` (mais o spread atual) a favor da posição.
  - `LossPoints` contra a posição.
- **Trailing stop** – Quando `TrailingPoints` é maior que zero, a estratégia registra o melhor preço alcançado após a entrada. Se o preço retroceder pela distância final, a posição será fechada.
- **Política de posição única** – Só é permitida uma posição de mercado por vez; novos sinais são ignorados enquanto uma negociação está em andamento ou enquanto as ordens de fechamento estão pendentes.

## Dimensionamento de posições
- **Volume fixo** – Quando `UseAutoLot` está desabilitado, cada negociação usa `FixedVolume` (ajustado ao passo e limites do volume do instrumento).
- **Cálculo automático de lote** – Quando ativado, a margem livre é dividida por `AutoMarginDivider * 1000` e arredondada para o lote inteiro mais próximo. Se pelo menos duas negociações perdedoras ocorreram consecutivamente, o volume é reduzido em `lossStreak / DecreaseFactor` proporcionalmente, imitando a lógica de redução do MT4.
- O volume é fixado entre `FixedVolume` e 99 lotes e depois ajustado aos limites de passo/min/máx de volume do instrumento.

## Notas adicionais
- O spread é lido a partir do melhor pedido/oferta atual e usado para ampliar a meta de lucro, correspondendo ao EA original.
- A margem livre é aproximada a partir do portfólio conectado (`CurrentValue - BlockedValue`), voltando ao patrimônio atual se os dados de margem não estiverem disponíveis.
- Todos os registros de tempo de execução, gráficos e ganchos de otimização são deixados para a infraestrutura padrão do StockSharp para que a estratégia possa ser otimizada por meio do designer ou executada diretamente no projeto API.

# QV EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Conversão do especialista MetaTrader "VQ_EA" que negocia utilizando o indicador Qualidade de Volatilidade (VQ).
- A versão StockSharp aproxima a linha VQ com um preço mediano suavizado para manter a lógica dentro do API de alto nível.
- As posições são abertas nas mudanças de direção da linha suavizada e gerenciadas com ordens de proteção opcionais.

## Comportamento original MQL
1. Solicita sinais de compra ou venda do indicador personalizado VQ (buffers 3 e 4).
2. Abre uma nova posição de mercado quando um novo sinal aparece e nenhuma negociação está ativa nessa direção.
3. Fecha a posição oposta imediatamente com um sinal oposto.
4. Recursos opcionais de gerenciamento de dinheiro: lotes fixos, lotes fracionários, ponto de equilíbrio, trailing stop, saída de registro manual e notificações de alerta/e-mail.

## StockSharp implementação
- Em vez do indicador proprietário VQ, a estratégia aplica uma média móvel simples ao preço médio e, opcionalmente, suaviza-o mais uma vez.
- A inclinação da série suavizada desempenha o papel da mudança de cor original da linha VQ.
- Um filtro configurável expresso em pontos evita sinais causados por pequenas flutuações.
- As ordens de mercado são usadas para entradas e saídas, refletindo o comportamento original EA.

### Geração de sinal
1. Assine o tipo de vela selecionado e calcule o preço médio de cada vela concluída.
2. Aplique a média móvel básica (`Length`) e, se solicitado, uma suavização adicional (`Smoothing`).
3. Compare o valor suavizado atual com o anterior. Se a variação absoluta exceder `FilterPoints` (convertida em unidades de preço), marque a direção como ascendente ou descendente.
4. Quando a direção muda de baixo para cima, uma entrada longa é emitida. Uma virada de cima para baixo produz uma entrada curta. As posições existentes são revertidas adicionando o volume absoluto da posição ao tamanho da ordem.

### Gestão de risco
- `StopLossPoints`, `TakeProfitPoints` e `TrailingStopPoints` são convertidos em preços absolutos multiplicando pela etapa de preço do instrumento.
- Se pelo menos uma dessas proteções estiver habilitada, `StartProtection` é chamado com ajustes de ordem de mercado para que os stops sigam a posição como no especialista MQL.
- O trailing stop opcional é ativado somente quando `UseTrailing` é `true` e a distância final é maior que zero.

## Parâmetros
- `Length` – período base de suavização do preço mediano. Padrão: 5.
- `Smoothing` – período de suavização secundário. Padrão: 1 (desativado).
- `FilterPoints` – movimento mínimo em pontos necessários para confirmar que a inclinação mudou. Padrão: 5.
- `StopLossPoints` – stop loss de proteção em pontos. Padrão: 60 (0 desativa).
- `TakeProfitPoints` – lucro protetor em pontos. Padrão: 0 (desabilitado).
- `UseTrailing` – ativa ou desativa os trailing stops. Padrão: falso.
- `TrailingStopPoints` – distância final em pontos. Padrão: 0 (ignorado quando `UseTrailing` é falso).
- `CandleType` – período usado para cálculos. Padrão: velas de 1 hora.
- `Volume` – herdado de `Strategy.Volume`, o padrão é 1 contrato e é usado para cada nova entrada.

## Diferenças do especialista original
- Os valores exatos do buffer VQ são aproximados pelos preços medianos suavizados; o indicador não é portado um para um.
- Recursos avançados, como turnos de ponto de equilíbrio, agendamento de alerta sonoro, saída manual de registros e gerenciamento de dinheiro de lote fracionário não são reproduzidos.
- O tratamento da etapa final é simplificado para o gerenciador de trailing stop integrado do StockSharp.

## Notas de uso
- Os sinais são gerados apenas em velas finalizadas, correspondendo ao modo "negociação no fechamento da barra" do EA original.
- Certifique-se de que o instrumento tenha um `PriceStep` adequado; caso contrário, a estratégia volta para um passo de 1,0 ao converter parâmetros baseados em pontos.
- A estratégia destina-se a demonstração e pode ser alargada com regras adicionais de gestão de dinheiro, se necessário.

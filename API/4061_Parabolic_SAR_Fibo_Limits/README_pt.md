# Parabolic SAR Limites Fibo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Parabolic SAR Fibo Limits é uma porta StockSharp do MetaTrader 4 consultor especialista `FT_0tk80i9uw4ep_Parabolic`. O robô original combina uma pilha dupla Parabolic SAR com níveis de retração Fibonacci para preparar entradas de limite nas principais zonas de pullback. A estratégia C# preserva a colocação de pedidos escalonados, as proteções integradas de ponto de equilíbrio e de trilha e o filtro de sessão de negociação opcional para que o comportamento corresponda à origem EA quando anexado a um gráfico com velas finalizadas.

## Lógica estratégica
### Preparação de sinal
* **Alinhamento duplo Parabolic SAR** – dois indicadores Parabolic SAR são calculados no mesmo período de tempo. O rápido SAR é usado como um aviso antecipado, enquanto o lento SAR confirma a mudança de estado. Quando o SAR rápido salta acima do preço enquanto o SAR lento permanece abaixo dele, a estratégia arma uma configuração longa em potencial. Quando o SAR rápido cai abaixo do preço enquanto o SAR lento permanece acima dele, uma possível configuração curta é armada. As configurações são apagadas assim que o SAR lento cruza o preço na respectiva direção.
* **Detecção de oscilação** – a estratégia consulta o máximo mais alto e o mínimo mais baixo na janela `Bar Search` configurável para replicar o auxiliar `MaximumMinimum` do EA. A vela finalizada anteriormente fornece o extremo oposto (`High[1]` ou `Low[1]`) que ancora os cálculos Fibonacci.

### Colocação e gerenciamento de pedidos
* **Fibonacci ordens pendentes** – uma vez que ambos os SARs estejam no mesmo lado do preço e uma configuração esteja armada, a estratégia envia uma ordem com limite no nível de 50% Fibonacci (`Entry Fibonacci %`) da oscilação detectada. O stop de proteção é compensado do extremo da oscilação pelo número configurado de pontos e o take-profit é colocado na projeção Fibonacci estendida (`Target Fibonacci %`). As ordens só são aceitas quando o preço atual, a parada planejada e o alvo estão a pelo menos cinco etapas de preço um do outro, refletindo o filtro de segurança `Point*5` do EA.
* **Limpeza automática de ordem** – sempre que o SAR rápido ultrapassa o preço, a ordem com limite pendente para essa direção é cancelada para evitar entrar na fase errada do mercado. O preenchimento de uma ordem com limite cancela automaticamente a ordem pendente oposta.

### Gestão de risco
* **Parada inicial e meta** – os parâmetros de stop-loss e take-profit da ordem pendente do EA são emulados aplicando os níveis de parada e meta calculados assim que a ordem com limite é preenchida.
* **Mudança de ponto de equilíbrio** – se `Break Even (points)` for maior que zero, o stop se move para o preço de entrada mais um passo de preço (ou menos um passo para posições vendidas) assim que a negociação ganhar o número especificado de pontos, reproduzindo a rotina original do BBU.
* **Trailing stop** – quando `Trailing Stop (points)` está habilitado, a parada segue o preço na distância escolhida. A parada só é atualizada quando a nova parada melhora a anterior em pelo menos `Trailing Step (points)`, correspondendo ao comportamento `TrailingShag` do EA.
* **Gatilhos de saída manuais** – se o preço atingir o stop calculado ou os níveis-alvo em uma vela finalizada, a posição é fechada com uma ordem de mercado para simular a execução automática da ordem do MT4.

### Filtro de tempo
* **Controle de sessão opcional** – a ativação de `Use Time Filter` restringe novas entradas à janela inclusiva entre `Start Hour` e `Stop Hour` no tempo de troca. A lógica de proteção (ponto de equilíbrio, trailing, saídas) continua a operar mesmo fora da sessão, assim como na implementação MQL.

## Parâmetros
* **Usar filtro de tempo** – alterna o filtro da sessão de negociação.
* **Hora de início / Hora de término** – horas de sessão incluídas quando o filtro de horário está ativado.
* **Fast SAR Step / Fast SAR Max** – fator de aceleração e aceleração máxima para o rápido Parabolic SAR.
* **Lento SAR Passo / Lento SAR Máx** – fator de aceleração e aceleração máxima para o lento Parabolic SAR.
* **Bar Search** – número de barras incluídas no cálculo do swing alto/baixo.
* **Offset (pontos)** – número de etapas de preço adicionadas além do extremo da oscilação ao calcular o stop-loss.
* **Entrada Fibonacci %** – Fibonacci porcentagem (expressa como 0–200+) usada para o preço do pedido com limite.
* **Alvo Fibonacci %** – Fibonacci porcentagem aplicada para calcular a projeção de lucro.
* **Break Even (pontos)** – lucro em pontos necessários antes do stop saltar para o preço de entrada (+/- um passo). Defina como `0` para desativar.
* **Trailing Stop (pontos)** – distância entre o preço e o trailing stop. Defina como `0` para desativar o rastreamento.
* **Trailing Step (pontos)** – melhoria mínima (em pontos) antes do trailing stop ser avançado.
* **Tipo de vela** – período de tempo que orienta os cálculos do indicador e da oscilação.
* **Volume** – volume do pedido base herdado da classe StockSharp `Strategy` (padrão `0.1`).

## Notas adicionais
* Todos os parâmetros baseados em pontos são automaticamente convertidos em compensações de preço usando a etapa de preço do instrumento. Símbolos FX de cinco dígitos, índices e outros ativos, portanto, reutilizam as configurações EA sem escalonamento manual.
* A estratégia processa apenas velas finalizadas fornecidas pela assinatura configurada, correspondendo exatamente à execução barra por barra do EA.
* Não existe uma versão Python desta estratégia; apenas a implementação C# está disponível no pacote API.

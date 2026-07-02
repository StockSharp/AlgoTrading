# Básico ATR Parar Tomar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Basic ATR Stop Take transporta o MetaTrader 4 consultor especialista **“Basic ATR stop_take consultor especialista”** para a estratégia de alto nível StockSharp API. O sistema é intencionalmente mínimo: ele abre exatamente uma posição de mercado na direção escolhida, calcula um intervalo médio verdadeiro (ATR) nas velas de trabalho e atribui níveis protetores de stop-loss e take-profit derivados de multiplicador ATRes. Assim que a negociação for fechada em qualquer nível, a estratégia se prepara imediatamente para a próxima configuração na mesma direção.

## Lógica estratégica
### Base do indicador
* **Average True Range (ATR)** – calculado no tipo de vela subscrito com um lookback configurável. O indicador mede a volatilidade recente e dimensiona as distâncias de stop e alvo.

### Regras de entrada
* É executado no fechamento de cada vela concluída após o ATR estar totalmente formado.
* Se nenhuma posição estiver aberta e o parâmetro de direção estiver definido como **Compra**, uma ordem de compra a mercado será enviada usando o volume configurado.
* Se nenhuma posição estiver aberta e o parâmetro de direção estiver definido como **Venda**, uma ordem de venda a mercado será enviada com o volume configurado.
* Escolher **Nenhum** desativa novas entradas enquanto mantém as posições existentes gerenciadas até que sejam fechadas.

### Regras de saída
* **ATR stop-loss** – a distância é igual a `ATR × Stop Factor`. Para compras, o stop é colocado abaixo da entrada; para shorts é colocado acima da entrada. Quando o extremo da vela ultrapassa o nível, a posição é fechada no mercado.
* **ATR take-profit** – a distância é igual a `ATR × Take Factor`. Para posições compradas, a meta de lucro fica acima da entrada; para shorts fica abaixo. Atingir o nível fecha a negociação no mercado.
* Se um dos multiplicadores estiver definido como `0`, o nível correspondente será desativado; a estratégia continua a monitorar o nível restante, se presente.

### Gestão de posição
* Apenas uma posição é permitida por vez. Após uma saída, a estratégia aguarda o fechamento da próxima vela antes de entrar novamente na mesma direção.
* `StartProtection()` é invocado durante a inicialização para que as posições manuais externas sejam monitoradas pelo subsistema de proteção StockSharp.

## Parâmetros
* **Direção de negociação** – lado do mercado para negociação (`None`, `Buy` ou `Sell`).
* **Volume de Negociação** – volume de pedidos para entrada no mercado único.
* **ATR Período** – número de velas usadas no cálculo de ATR.
* **Stop Factor** – multiplicador ATR aplicado à distância stop-loss. Zero desativa a parada protetora.
* **Take Factor** – multiplicador ATR aplicado à distância de take-profit. Zero desativa a meta de lucro.
* **Tipo de vela** – período de tempo das velas usadas para cálculo de ATR e gerenciamento de negociação.

## Notas adicionais
* Os parâmetros padrão replicam o comportamento do EA (modo somente longo, volume de lote 0,01, período ATR 14, fator de parada 1,5, fator de aceitação 2,0).
* As comparações de preços usam máximos e mínimos de velas, o que significa que os gatilhos de stop-loss e take-profit ocorrem assim que o nível é ultrapassado dentro da faixa da vela.
* A estratégia não empilha nem inverte posições; em vez disso, ele sempre fica nivelado e aguarda o fechamento da próxima barra antes de fazer um novo pedido.
* Somente a implementação C# é fornecida neste pacote; não existe uma versão Python para esta estratégia.

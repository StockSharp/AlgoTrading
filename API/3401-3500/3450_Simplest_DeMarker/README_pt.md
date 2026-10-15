# Estratégia DeMarker mais simples
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia DeMarker mais simples reproduz a lógica do consultor especialista MetaTrader original. Ele rastreia o oscilador DeMarker para detectar quando a dinâmica do preço sai das zonas de sobrecompra ou sobrevenda. Quando o oscilador volta para dentro da faixa neutra, a estratégia abre uma posição na direção da reversão esperada enquanto gerencia o risco por meio de distâncias configuráveis ​​de stop-loss e take-profit.

## Lógica principal
1. Assine as velas do período selecionado e calcule o indicador DeMarker com o período configurado.
2. Marque o estado do mercado como **sobrecompra** sempre que o valor anterior do DeMarker estiver acima do limite de sobrecompra e como **sobrevenda** quando estiver abaixo do limite de sobrevenda.
3. Gere sinais quando o valor atual do DeMarker cruzar de volta para dentro da área neutra:
   - Venda quando o oscilador cair abaixo do nível de sobrecompra, depois de anteriormente estar acima dele.
   - Compre quando o oscilador subir acima do nível de sobrevenda, depois de anteriormente estar abaixo dele.
4. Coloque apenas uma posição de cada vez. Se `Trade On Bar Open` estiver ativado, o pedido será adiado até a próxima barra abrir; caso contrário, a posição será inserida imediatamente no fechamento da barra atual.
5. Aplique ordens stop-loss e take-profit usando o serviço de proteção integrado para imitar as distâncias fixas da versão MQL.

## Parâmetros
- **Volume** – tamanho do pedido em lotes/contratos.
- **Período DeMarker** – período do oscilador DeMarker.
- **Nível de sobrecompra** – limite superior do DeMarker que define condições de sobrecompra.
- **Nível de sobrevenda** – limite inferior do DeMarker que define condições de sobrevenda.
- **Trade On Bar Open** – se ativado, as entradas são executadas na próxima barra aberta, e não imediatamente.
- **Pontos de Stop Loss** – distância protetora de stop-loss expressa em pontos de preço.
- **Take Profit Points** – distância alvo de lucro expressa em faixas de preço.
- **Tipo de vela** – tipo de vela (período de tempo) usado para cálculos de indicadores.

## Gestão de capital
- As ordens stop-loss e take-profit são registradas automaticamente por meio de `StartProtection` com distâncias convertidas em faixas de preço.
- Apenas uma posição pode estar ativa por vez. Novos sinais são ignorados enquanto existe uma posição.

## Elementos do gráfico
- Velas de preço para a assinatura selecionada.
- A curva do indicador DeMarker.
- Marcadores de negociações próprios para validação visual de entradas e saídas.

## Notas
- Utilize instrumentos de liquidez suficientemente elevados para garantir a qualidade da execução do stop-loss e do take-profit.
- O sinalizador `Trade On Bar Open` se aproxima do comportamento original do consultor especialista que aguarda uma nova barra antes de enviar o pedido.
